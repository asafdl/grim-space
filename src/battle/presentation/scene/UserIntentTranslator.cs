using Godot;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Presentation.Picking;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Presentation.Camera;
using GrimSpace.Battle.Abilities;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Scene;

/// <summary>
/// Maps HUD and world user input to <see cref="IActionSink"/> commands.
/// Does not own presentation state or apply frames.
/// </summary>
public sealed partial class UserIntentTranslator : Node
{
	private readonly string _actorId;
	private readonly IActionSink _actions;
	private readonly Controller _camera;
	private readonly BattleHud _hud;
	private readonly AbilitySourcePickerView _abilitySourcePicker;
	private readonly Func<IReadOnlyDictionary<string, UnitView>> _unitViews;

	private bool _enabled;
	private bool _canIssueActions;
	private bool _isInspecting;
	private EPlayerMode _mode = EPlayerMode.Move;
	private IReadOnlyList<MovePathOption> _moveOptions = [];
	private IReadOnlyList<AbilityActivationChoice> _abilityChoices = [];
	private int? _abilityHoveredIndex;
	private int? _moveHoveredIndex;
	private MoveInputSnapshot _moveInput;

	private readonly record struct MoveInputSnapshot(
		MovePathOption? Selected,
		Coord? Destination,
		IReadOnlyList<GridBasis> ReachableBases,
		IReadOnlyList<Coord> ReachableHeadings,
		bool IsDragging);

	public UserIntentTranslator(
		string actorId,
		IActionSink actions,
		Controller camera,
		BattleHud hud,
		AbilitySourcePickerView abilitySourcePicker,
		Func<IReadOnlyDictionary<string, UnitView>> unitViews)
	{
		_actorId = actorId;
		_actions = actions;
		_camera = camera;
		_hud = hud;
		_abilitySourcePicker = abilitySourcePicker;
		_unitViews = unitViews;
	}

	public event Action<EPlayerMode>? ModeRequested;
	public event Action<int?, int>? MoveHoverChanged;
	public event Action<Coord, GridBasis>? MoveSelectionStarted;
	public event Action<GridBasis>? MovePoseRequested;
	public event Action? MoveSelectionCanceled;
	public event Action<int?, int>? AbilityHoverChanged;
	public event Action? HoversCleared;
	public event Action<string>? FocusUnitRequested;
	public event Action? ReturnToPlayerRequested;
	public event Action? FocusCameraRequested;
	public event Action? UndoRequested;
	public event Action? EndTurnRequested;
	public event Action? ActionFailed;
	public event Action? RestartRequested;
	public event Action? RetireRequested;

	public void SetPresentation(
		bool enabled,
		bool canIssueActions,
		bool isInspecting,
		EPlayerMode mode,
		IReadOnlyList<MovePathOption> moveOptions,
		MovePathOption? selectedMove,
		Coord? moveDestination,
		bool moveDragging,
		IReadOnlyList<AbilityActivationChoice> abilityChoices,
		int? abilityHoveredIndex)
	{
		_enabled = enabled;
		_canIssueActions = canIssueActions;
		_isInspecting = isInspecting;
		_mode = mode;
		_moveOptions = moveOptions;
		_abilityChoices = abilityChoices;
		_abilityHoveredIndex = abilityHoveredIndex;
		var reachableBases = moveDestination is { } destination
			? moveOptions
				.Where(option => option.EndPosition == destination)
				.Select(option => option.EndBasis)
				.Distinct()
				.ToList()
			: [];
		_moveInput = new MoveInputSnapshot(
			selectedMove,
			moveDestination,
			reachableBases,
			reachableBases
			.Select(basis => basis.Forward)
			.Distinct()
			.ToList(),
			moveDragging);

		if (!enabled || mode != EPlayerMode.Move)
			_moveHoveredIndex = null;
		_camera.SetGestureInputBlocked(enabled && mode == EPlayerMode.Move && moveDragging);
	}
	public override void _Process(double delta)
	{
		if (!_enabled
			|| !_canIssueActions
			|| _moveInput.IsDragging)
			return;

		if (_mode != EPlayerMode.Move)
		{
			UpdateAbilityHover();
			return;
		}

		if (_hud.IsPauseMenuOpen || IsPointerOverHud())
		{
			ClearMoveHover();
			return;
		}

		if (_camera.IsManualGestureActive)
			return;

		var index = MovementSelection.PickPathIndex(
			_camera,
			GetViewport().GetMousePosition(),
			_moveOptions);
		if (index == _moveHoveredIndex)
			return;

		_moveHoveredIndex = index;
		MoveHoverChanged?.Invoke(index, _moveOptions.Count);
	}

	public override void _Input(InputEvent @event)
	{
		if (!_enabled)
			return;

		if (_moveInput.IsDragging)
		{
			switch (@event)
			{
				case InputEventMouseMotion motion:
					if (_moveInput.Destination is { } destination
						&& _moveInput.Selected is { } selected
						&& MovementSelection.PickHeading(
							_camera,
							motion.Position,
							destination,
							_moveInput.ReachableHeadings) is { } heading
						&& MovePose.SelectHeading(_moveInput.ReachableBases, selected.EndBasis, heading) is { } basis
						&& basis != selected.EndBasis)
						MovePoseRequested?.Invoke(basis);
					GetViewport().SetInputAsHandled();
					return;
				case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp }:
					RequestRoll(1);
					GetViewport().SetInputAsHandled();
					return;
				case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelDown }:
					RequestRoll(-1);
					GetViewport().SetInputAsHandled();
					return;
				case InputEventMouseButton { Pressed: false, ButtonIndex: MouseButton.Left }:
					QueueMoveSelection();
					GetViewport().SetInputAsHandled();
					return;
				case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right }:
					CancelMoveSelection();
					GetViewport().SetInputAsHandled();
					return;
			}
		}

		if (@event is InputEventKey
			{
				Pressed: true,
				Echo: false,
				Keycode: Key.Z
			} key
			&& (key.CtrlPressed || key.MetaPressed)
			&& _hud.UtilityBar.TryUndo())
		{
			GetViewport().SetInputAsHandled();
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_enabled)
			return;

		if (@event is InputEventKey { Pressed: true, Echo: false } key)
		{
			if (HandleKey(key))
				GetViewport().SetInputAsHandled();
			return;
		}

		if (_hud.IsPauseMenuOpen)
			return;

		if (@event is InputEventMouseButton
			{
				Pressed: true,
				ButtonIndex: MouseButton.Right
			})
		{
			if (_moveInput.Destination is not null)
			{
				CancelMoveSelection();
				GetViewport().SetInputAsHandled();
			}
			else if (TryCancelAbilityMode())
				GetViewport().SetInputAsHandled();
			return;
		}

		if (!_canIssueActions && !_isInspecting)
			return;

		if (@event is InputEventMouseButton
			{
				Pressed: true,
				ButtonIndex: MouseButton.Left
			} click)
		{
			HandleClick(click.Position);
		}
	}

	public void OnMoveMode() => ModeRequested?.Invoke(EPlayerMode.Move);

	public void OnEndTurn()
	{
		if (_moveInput.Destination is null)
			EndTurnRequested?.Invoke();
	}

	public void OnUndo()
	{
		_moveHoveredIndex = null;
		UndoRequested?.Invoke();
	}

	public void OnFocusCamera() => FocusCameraRequested?.Invoke();

	public void OnReturnToPlayer() => ReturnToPlayerRequested?.Invoke();

	public void OnRestart() => RestartRequested?.Invoke();

	public void OnRetire() => RetireRequested?.Invoke();

	private bool HandleKey(InputEventKey key)
	{
		switch (key.Keycode)
		{
			case Key.Escape when _hud.IsPauseMenuOpen:
				_hud.ClosePauseMenu();
				return true;
			case Key.Escape when _mode != EPlayerMode.Move:
				return TryCancelAbilityMode();
			case Key.Escape when _moveInput.Destination is not null:
				CancelMoveSelection();
				return true;
			case Key.Escape:
				ClearMoveHover();
				_hud.TogglePauseMenu();
				return true;
			case Key.Key1:
				return _hud.ManeuverBar.TryActivateMove();
			case Key.Key2:
				return _hud.ActionBar.TryActivateHotkey(2);
			case Key.Key3:
				return _hud.ActionBar.TryActivateHotkey(3);
			case Key.Key4:
				return _hud.ActionBar.TryActivateHotkey(4);
			case Key.Space:
				OnEndTurn();
				return true;
			case Key.F:
				return _hud.UtilityBar.TryFocus();
			default:
				return false;
		}
	}

	private bool TryCancelAbilityMode()
	{
		if (_mode == EPlayerMode.Move)
			return false;

		ModeRequested?.Invoke(EPlayerMode.Move);
		return true;
	}

	private void UpdateAbilityHover()
	{
		var abilityHoveredIndex = _hud.IsPauseMenuOpen
			|| _camera.IsManualGestureActive
			|| IsPointerOverHud()
			? null
			: _abilitySourcePicker.PickIndex(GetViewport().GetMousePosition());
		if (abilityHoveredIndex != _abilityHoveredIndex)
		{
			_abilityHoveredIndex = abilityHoveredIndex;
			AbilityHoverChanged?.Invoke(abilityHoveredIndex, _abilityChoices.Count);
		}
	}

	private void HandleClick(Vector2 screenPosition)
	{
		if (_mode == EPlayerMode.Move)
		{
			HandleMoveClick(screenPosition);
			return;
		}

		if (!_canIssueActions)
			return;

		var index = _abilitySourcePicker.PickIndex(screenPosition);
		if (index is not int choiceIndex
			|| choiceIndex < 0
			|| choiceIndex >= _abilityChoices.Count)
		{
			return;
		}

		var action = AbilityActivation.CreateExecutionAction(_abilityChoices[choiceIndex]);
		if (!Enqueue(action))
		{
			ActionFailed?.Invoke();
			return;
		}

		ModeRequested?.Invoke(EPlayerMode.Move);
	}

	private void HandleMoveClick(Vector2 screenPosition)
	{
		if (UnitPick.Pick(_camera, screenPosition, _unitViews()) is { } unitId)
		{
			if (unitId != _actorId)
			{
				FocusUnitRequested?.Invoke(unitId);
				_moveHoveredIndex = null;
				return;
			}

			if (_isInspecting)
			{
				ReturnToPlayerRequested?.Invoke();
				return;
			}
		}

		if (_isInspecting || !_canIssueActions)
			return;

		if (MovementSelection.PickPathIndex(_camera, screenPosition, _moveOptions) is not int index)
		{
			PresentationDiagnostics.LogMovePickMiss(_moveOptions.Count);
			return;
		}

		var steps = _moveOptions[index].Steps;
		if (steps.Count == 0)
			return;
		var selected = _moveOptions[index];
		MoveSelectionStarted?.Invoke(selected.EndPosition, selected.EndBasis);
		_moveHoveredIndex = null;
		ClearHovers();
	}

	private void CancelMoveSelection() => MoveSelectionCanceled?.Invoke();

	private void QueueMoveSelection()
	{
		if (_moveInput.Selected is not { } selected)
			return;

		if (!_actions.TryEnqueue(selected.Steps.Cast<IAction>().ToArray()))
		{
			ActionFailed?.Invoke();
			return;
		}

		CancelMoveSelection();
	}

	private void RequestRoll(int delta)
	{
		if (_moveInput.Selected is not { } selected
			|| MovePose.CycleRoll(_moveInput.ReachableBases, selected.EndBasis, delta) is not { } basis
			|| basis == selected.EndBasis)
			return;

		MovePoseRequested?.Invoke(basis);
	}

	private bool Enqueue(params IAction[] actions) =>
		_canIssueActions && _actions.TryEnqueue(actions);

	private bool IsPointerOverHud() =>
		GetViewport().GuiGetHoveredControl() is { } hovered && _hud.IsAncestorOf(hovered);

	private void ClearMoveHover()
	{
		if (_moveHoveredIndex is null)
			return;

		_moveHoveredIndex = null;
		MoveHoverChanged?.Invoke(null, _moveOptions.Count);
	}

	private void ClearHovers() => HoversCleared?.Invoke();
}
