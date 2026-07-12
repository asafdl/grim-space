using Godot;
using GrimSpace.Battle.Presentation.Camera;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Picking;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Scene;

/// <summary>
/// Thin scene connector: wires Godot input and nodes to <see cref="BattlePresenter"/> and <see cref="Manager"/>.
/// </summary>
public partial class BattleController : Node3D
{
	private BattlePresenter _presenter = null!;

	private GridView _gridView = null!;
	private Controller _camera = null!;
	private Label _hintLabel = null!;
	private ActionBar _actionBar = null!;
	private ShipOrientationHud _orientationHud = null!;
	private MissileRangeIndicator _missileRangeIndicator = null!;

	private readonly Dictionary<string, UnitView> _unitViews = new();

	public override void _Ready()
	{
		var manager = Manager.FromEncounter(Session.Instance.CurrentEncounter);
		_presenter = new BattlePresenter(manager);

		var backdrop = new SpaceBackdrop();
		AddChild(backdrop);
		MoveChild(backdrop, 0);

		_camera = GetNode<Controller>("Camera3D");
		_gridView = GetNode<GridView>("GridView");
		_gridView.Build(manager.Grid);
		_camera.SetPivot(WorldMapping.GridCenter(manager.Grid));

		_missileRangeIndicator = new MissileRangeIndicator();
		AddChild(_missileRangeIndicator);

		var unitsRoot = GetNode<Node3D>("Units");
		foreach (var unit in manager.Units)
		{
			var view = new UnitView();
			view.Bind(unit.State, ColorFor(unit.Controller));
			unitsRoot.AddChild(view);
			_unitViews[unit.State.Id] = view;
		}

		SetupHintLabel();
		SetupActionBar();
		SetupOrientationHud();
		Refresh();
	}

	private void SetupOrientationHud()
	{
		_orientationHud = new ShipOrientationHud();
		_orientationHud.HeadingTurnRequested += turn =>
		{
			if (_presenter.TryQueueHeadingTurn(turn))
				Refresh();
		};
		_orientationHud.RollRequested += direction =>
		{
			if (_presenter.TryQueueRoll(direction))
				Refresh();
		};
		AddChild(_orientationHud);
	}

	private void SetupActionBar()
	{
		_actionBar = new ActionBar();
		_actionBar.ModeChanged += mode => { _presenter.SetMode(mode); ExitAimIfNeeded(); Refresh(); };
		_actionBar.MissileMountSelected += mount => { _presenter.SelectMissileMount(mount); Refresh(); };
		_actionBar.EndTurnRequested += () => { if (_presenter.EndTurn()) { ExitAimIfNeeded(); Refresh(); } };
		AddChild(_actionBar);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
		{
			if (_presenter.Mode == EPlayerMode.Missile)
			{
				_presenter.CancelMissileMode();
				_camera.ExitAim();
				Refresh();
			}

			GetViewport().SetInputAsHandled();
			return;
		}

		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Z } key
			&& (key.CtrlPressed || key.MetaPressed))
		{
			if (_presenter.Undo())
				Refresh();

			GetViewport().SetInputAsHandled();
			return;
		}

		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Space })
		{
			if (_presenter.EndTurn())
			{
				_camera.ExitAim();
				Refresh();
			}

			GetViewport().SetInputAsHandled();
			return;
		}

		var frame = _presenter.BuildFrame();
		if (_presenter.Manager.IsBattleOver || frame.ActiveUnit is null)
			return;

		if (@event is InputEventMouseMotion motion)
		{
			HandleMouseMotion(motion.Position, frame);
			return;
		}

		if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click)
			return;

		HandleLeftClick(click.Position, frame);
	}

	private void HandleMouseMotion(Vector2 screenPos, PresentationFrame frame)
	{
		switch (_presenter.Mode)
		{
			case EPlayerMode.Move:
				_presenter.SetMoveHover(
					MovementSelection.PickOptionIndex(_camera, screenPos, frame.MoveOptions),
					frame.MoveOptions.Count);
				break;

			case EPlayerMode.Missile:
				_presenter.SetMissileHover(
					GridPick.PickFromSet(_camera, screenPos, frame.ValidMissileCells));
				break;

			case EPlayerMode.Railgun:
				var picked = GridPick.PickUnit(_camera, screenPos, _presenter.Manager.Units);
				_presenter.SetRailgunHover(picked);
				break;
		}

		Refresh();
	}

	private void HandleLeftClick(Vector2 screenPos, PresentationFrame frame)
	{
		switch (_presenter.Mode)
		{
			case EPlayerMode.Move:
				if (MovementSelection.PickOptionIndex(_camera, screenPos, frame.MoveOptions) is int index)
					_presenter.TryQueueMove(index, frame.MoveOptions);
				break;

			case EPlayerMode.Missile:
				if (GridPick.PickFromSet(_camera, screenPos, frame.ValidMissileCells) is { } center)
					_presenter.TryQueueMissile(center);
				break;

			case EPlayerMode.Railgun:
				var target = GridPick.PickUnit(_camera, screenPos, _presenter.Manager.Units);
				if (target is not null)
					_presenter.TryQueueRailgun(target);
				break;
		}

		Refresh();
	}

	private void Refresh()
	{
		var frame = _presenter.BuildFrame();
		ApplyFrame(frame);
	}

	private void ApplyFrame(PresentationFrame frame)
	{
		ApplyUnitViews(frame);
		ApplyCamera(frame);
		ApplyGrid(frame);
		ApplyActionBar(frame);
		ApplyOrientationHud(frame);
		_hintLabel.Text = frame.HintText;
	}

	private void ApplyOrientationHud(PresentationFrame frame)
	{
		_orientationHud.Show(frame.CanAct && !frame.MissileAimActive);
	}

	private void ApplyUnitViews(PresentationFrame frame)
	{
		foreach (var unit in _presenter.Manager.Units)
		{
			var display = unit.Controller == EController.Player
				? frame.Simulation.Player
				: frame.Simulation.Enemy;
			_unitViews[unit.State.Id].SyncFromState(display);
		}
	}

	private void ApplyCamera(PresentationFrame frame)
	{
		if (frame.MissileAimActive && frame.MissileAimShip is not null)
			_camera.EnterDorsalAim(frame.MissileAimShip);
		else if (frame.ExitMissileMode)
			_camera.ExitAim();
	}

	private void ApplyGrid(PresentationFrame frame)
	{
		if (frame.ActiveUnit is null || _presenter.Manager.IsBattleOver)
		{
			_gridView.ClearHighlights();
			_missileRangeIndicator.SetActive(null, 0);
			return;
		}

		_missileRangeIndicator.SetActive(null, 0);

		switch (frame.Mode)
		{
			case EPlayerMode.Move:
				_gridView.SetMoveHighlights(
					frame.MoveOptions,
					frame.MovePath,
					frame.MoveTarget,
					frame.PlannedHazardCells);
				break;

			case EPlayerMode.Missile:
				_gridView.SetMissileHighlights(
					frame.PlannedHazardCells,
					frame.ValidMissileCells,
					frame.MissilePreviewCells);
				break;

			case EPlayerMode.Railgun:
				_gridView.SetRailgunHighlights(
					frame.RailgunTargetCells,
					frame.RailgunHoveredCell,
					frame.PlannedHazardCells);
				break;
		}
	}

	private void ApplyActionBar(PresentationFrame frame)
	{
		_actionBar.SetMode(frame.Mode, frame.MissileMount);
		_actionBar.Configure(
			frame.MissilesRemaining,
			CombatConfig.MissilesPerTurn,
			frame.CanAct);
	}

	private void ExitAimIfNeeded()
	{
		if (_presenter.Mode != EPlayerMode.Missile)
			_camera.ExitAim();
	}

	private static Color ColorFor(EController controller) =>
		controller switch
		{
			EController.Player => new Color(0.25f, 0.85f, 0.35f),
			EController.Enemy => new Color(0.9f, 0.25f, 0.2f),
			_ => Colors.White,
		};

	private void SetupHintLabel()
	{
		var canvas = new CanvasLayer();
		_hintLabel = new Label { Position = new Vector2(16, 16) };
		canvas.AddChild(_hintLabel);
		AddChild(canvas);
	}
}
