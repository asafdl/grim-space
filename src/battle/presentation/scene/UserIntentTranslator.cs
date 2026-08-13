using Godot;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Picking;
using GrimSpace.Battle.Presentation.Ui;
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
	private readonly Camera3D _camera;
	private readonly BattleHud _hud;
	private readonly FlakPreviewView _flakPreview;
	private readonly RailgunPreviewView _railgunPreview;
	private readonly Func<IReadOnlyDictionary<string, UnitView>> _unitViews;

	private bool _enabled;
	private bool _canIssueActions;
	private bool _isInspecting;
	private EPlayerMode _mode = EPlayerMode.Move;
	private IReadOnlyList<MovePathOption> _moveOptions = [];
	private UnitDisplayState? _focusState;
	private int? _moveHoveredIndex;

	public UserIntentTranslator(
		string actorId,
		IActionSink actions,
		Camera3D camera,
		BattleHud hud,
		FlakPreviewView flakPreview,
		RailgunPreviewView railgunPreview,
		Func<IReadOnlyDictionary<string, UnitView>> unitViews)
	{
		_actorId = actorId;
		_actions = actions;
		_camera = camera;
		_hud = hud;
		_flakPreview = flakPreview;
		_railgunPreview = railgunPreview;
		_unitViews = unitViews;
	}

	public event Action<EPlayerMode>? ModeRequested;
	public event Action<int?, int>? MoveHoverChanged;
	public event Action<ESpatialOrientation?>? FlakHoverChanged;
	public event Action<bool>? RailgunHoverChanged;
	public event Action<ESpatialOrientation?>? TorpedoHoverChanged;
	public event Action? HoversCleared;
	public event Action<string>? FocusUnitRequested;
	public event Action? ReturnToPlayerRequested;
	public event Action? FocusCameraRequested;
	public event Action? EndTurnRequested;
	public event Action? RestartRequested;
	public event Action? RetireRequested;

	public void SetPresentation(
		bool enabled,
		bool canIssueActions,
		bool isInspecting,
		EPlayerMode mode,
		IReadOnlyList<MovePathOption> moveOptions,
		UnitDisplayState focusState)
	{
		_enabled = enabled;
		_canIssueActions = canIssueActions;
		_isInspecting = isInspecting;
		_mode = mode;
		_moveOptions = moveOptions;
		_focusState = focusState;

		if (!enabled || mode != EPlayerMode.Move)
			_moveHoveredIndex = null;
	}

	public override void _Process(double delta)
	{
		if (!_enabled || !_canIssueActions || _mode != EPlayerMode.Move)
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

		if (!_canIssueActions && !_isInspecting)
			return;

		if (@event is InputEventMouseMotion motion)
		{
			HandleHover(motion.Position);
			return;
		}

		if (@event is InputEventMouseButton
			{
				Pressed: true,
				ButtonIndex: MouseButton.Left
			} click)
		{
			HandleClick(click.Position);
		}
	}

	public void OnYaw()
	{
		if (Enqueue(new HeadingTurnAction(_actorId, EHeadingTurn.YawRight)))
			ClearHovers();
	}

	public void OnSpin()
	{
		if (Enqueue(new RollAction(_actorId, ERollDirection.Clockwise)))
			ClearHovers();
	}

	public void OnMoveMode() => ModeRequested?.Invoke(EPlayerMode.Move);

	public void OnEndTurn() => EndTurnRequested?.Invoke();

	public void OnUndo()
	{
		if (!_actions.Undo())
			return;

		_moveHoveredIndex = null;
		ClearHovers();
	}

	public void OnFocusCamera() => FocusCameraRequested?.Invoke();

	public void OnReturnToPlayer() => ReturnToPlayerRequested?.Invoke();

	public void OnRestart() => RestartRequested?.Invoke();

	public void OnRetire() => RetireRequested?.Invoke();

	private bool HandleKey(InputEventKey key)
	{
		switch (key.Keycode)
		{
			case Key.Escape when _mode is EPlayerMode.Flak or EPlayerMode.Railgun or EPlayerMode.Torpedo or EPlayerMode.Detonate:
				ModeRequested?.Invoke(EPlayerMode.Move);
				return true;
			case Key.Key1:
				return _hud.ManeuverBar.TryActivateMove();
			case Key.Key2:
				return _hud.ActionBar.TryActivateHotkey(2);
			case Key.Key3:
				return _hud.ActionBar.TryActivateHotkey(3);
			case Key.Key4:
				return _hud.ActionBar.TryActivateHotkey(4);
			case Key.Q:
				return _hud.ManeuverBar.TrySpin();
			case Key.E:
				return _hud.ManeuverBar.TryYaw();
			case Key.Space:
				EndTurnRequested?.Invoke();
				return true;
			case Key.F:
				return _hud.UtilityBar.TryFocus();
			default:
				return false;
		}
	}

	private void HandleHover(Vector2 screenPosition)
	{
		if (!_canIssueActions)
			return;

		switch (_mode)
		{
			case EPlayerMode.Flak:
				FlakHoverChanged?.Invoke(
					_flakPreview.PickMountedOn(_camera, screenPosition));
				break;
			case EPlayerMode.Railgun:
				RailgunHoverChanged?.Invoke(
					_railgunPreview.PickHovered(_camera, screenPosition));
				break;
			case EPlayerMode.Torpedo:
				TorpedoHoverChanged?.Invoke(PickTorpedoMountedOn(screenPosition));
				break;
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

		switch (_mode)
		{
			case EPlayerMode.Flak:
				if (_flakPreview.PickMountedOn(_camera, screenPosition) is ESpatialOrientation mountedOn
					&& Enqueue(new FlakAction(_actorId, mountedOn)))
				{
					ClearHovers();
					ModeRequested?.Invoke(EPlayerMode.Move);
				}
				break;

			case EPlayerMode.Railgun:
				if (_railgunPreview.PickHovered(_camera, screenPosition)
					&& Enqueue(new RailgunAction(_actorId)))
				{
					ClearHovers();
					ModeRequested?.Invoke(EPlayerMode.Move);
				}
				break;

			case EPlayerMode.Torpedo:
				if (PickTorpedoMountedOn(screenPosition) is ESpatialOrientation torpedoMountedOn
					&& Enqueue(new TorpedoAction(_actorId, torpedoMountedOn)))
				{
					ClearHovers();
					ModeRequested?.Invoke(EPlayerMode.Move);
				}
				break;
		}
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

		var directions = _moveOptions[index].Directions;
		if (directions.Count == 0
			|| !Enqueue(directions
				.Select(direction => (IAction)new MoveStepAction(_actorId, direction))
				.ToArray()))
		{
			PresentationDiagnostics.LogMoveQueueDetail(
				"queue_failed",
				_moveOptions[index].EndPosition);
			return;
		}

		_moveHoveredIndex = null;
		ClearHovers();
	}

	private ESpatialOrientation? PickTorpedoMountedOn(Vector2 screenPosition)
	{
		if (_focusState is null)
			return null;

		var ship = _focusState.ToState();
		var cells = new Dictionary<Coord, ESpatialOrientation>();
		foreach (var mountedOn in TorpedoMountedDirections)
		{
			var (position, _, _) = TorpedoMount.LaunchPose(ship, mountedOn);
			cells[position] = mountedOn;
		}

		return GridPick.PickFromSet(_camera, screenPosition, cells.Keys.ToHashSet()) is { } cell
			? cells[cell]
			: null;
	}

	private static readonly ESpatialOrientation[] TorpedoMountedDirections =
	[
		ESpatialOrientation.Retro,
		ESpatialOrientation.Ventral,
		ESpatialOrientation.Dorsal,
	];

	private bool Enqueue(params IAction[] actions) =>
		_canIssueActions && _actions.TryEnqueue(actions);

	private void ClearHovers() => HoversCleared?.Invoke();
}
