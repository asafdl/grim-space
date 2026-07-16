using Godot;
using GrimSpace.Battle.Presentation.Camera;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Picking;
using GrimSpace.Battle.Presentation.Events;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Scene;

/// <summary>
/// Thin scene connector: wires Godot input and nodes to <see cref="BattlePresenter"/> and <see cref="Manager"/>.
/// </summary>
public partial class BattleController : Node3D, IPresentationEventSink
{
	private BattlePresenter _presenter = null!;

	private GridView _gridView = null!;
	private Controller _camera = null!;
	private Label _hintLabel = null!;
	private ActionBar _actionBar = null!;
	private BattleOutcomeOverlay _outcomeOverlay = null!;
	private ShipOrientationHud _orientationHud = null!;
	private MissileRangeIndicator _missileRangeIndicator = null!;

	private readonly Dictionary<string, UnitView> _unitViews = new();
	private int? _lastHoveredMoveIndex;

	public override void _Ready()
	{
		GameLog.Logger = new GodotGameLogger();

		var manager = Manager.FromEncounter(Session.Instance.CurrentEncounter);
		_presenter = new BattlePresenter(manager);

		var backdrop = new SpaceBackdrop();
		backdrop.Build(manager.Grid);
		AddChild(backdrop);
		MoveChild(backdrop, 0);

		_camera = GetNode<Controller>("Camera3D");
		_gridView = GetNode<GridView>("GridView");
		_gridView.Build(manager.Grid);

		var gridCenter = WorldMapping.GridCenter(manager.Grid);
		_camera.SetPivot(gridCenter);
		var chamberRadius = manager.Grid.Width * WorldMapping.CellSize * 0.5f;
		RedDwarfSun.Configure(GetNode<DirectionalLight3D>("DirectionalLight3D"), gridCenter, chamberRadius);

		var hazardsRoot = new Node3D { Name = "BoardHazards" };
		AddChild(hazardsRoot);
		var hazardView = new BoardHazardView();
		hazardView.Build(manager.Hazards.Board);
		hazardsRoot.AddChild(hazardView);

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
		SetupOutcomeOverlay();
		SetupOrientationHud();
		Refresh();
	}

	public override void _Process(double _)
	{
		if (_presenter.Manager.IsResolving
			|| _presenter.Mode != EPlayerMode.Move
			|| _presenter.Manager.IsBattleOver
			|| _presenter.Manager.Player.GetActiveActor() is null)
		{
			_lastHoveredMoveIndex = null;
			return;
		}

		var frame = _presenter.BuildFrame();
		var index = MovementSelection.PickOptionIndex(_camera, GetViewport().GetMousePosition(), frame.MoveOptions);
		if (index == _lastHoveredMoveIndex)
			return;

		_lastHoveredMoveIndex = index;
		_presenter.SetMoveHover(index, frame.MoveOptions.Count);
		Refresh();
	}

	private void SetupOrientationHud()
	{
		_orientationHud = new ShipOrientationHud();
		_orientationHud.HeadingTurnRequested += turn =>
		{
			if (_presenter.TryQueueHeadingTurn(turn))
			{
				_lastHoveredMoveIndex = null;
				Refresh();
			}
		};
		_orientationHud.RollRequested += direction =>
		{
			if (_presenter.TryQueueRoll(direction))
			{
				_lastHoveredMoveIndex = null;
				Refresh();
			}
		};
		AddChild(_orientationHud);
	}

	private void SetupOutcomeOverlay()
	{
		_outcomeOverlay = new BattleOutcomeOverlay();
		_outcomeOverlay.ResetRequested += ResetBattle;
		AddChild(_outcomeOverlay);
	}

	private void SetupActionBar()
	{
		_actionBar = new ActionBar();
		_actionBar.ModeChanged += mode => { _presenter.SetMode(mode); ExitAimIfNeeded(); Refresh(); };
		_actionBar.MissileMountSelected += mount => { _presenter.SelectMissileMount(mount); Refresh(); };
		_actionBar.EndTurnRequested += () => { if (_presenter.EndTurn(this)) { ExitAimIfNeeded(); Refresh(); } };
		AddChild(_actionBar);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_presenter.Manager.IsBattleOver)
			return;

		if (_presenter.Manager.IsResolving)
			return;

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
			{
				_lastHoveredMoveIndex = null;
				Refresh();
			}

			GetViewport().SetInputAsHandled();
			return;
		}

		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Space })
		{
			if (_presenter.EndTurn(this))
			{
				_camera.ExitAim();
				Refresh();
			}

			GetViewport().SetInputAsHandled();
			return;
		}

		if (_presenter.Mode == EPlayerMode.Missile
			&& @event is InputEventMouseButton { Pressed: true } scroll
			&& scroll.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
		{
			var delta = scroll.ButtonIndex == MouseButton.WheelUp ? 1 : -1;
			if (_presenter.AdjustMissileRange(delta))
				Refresh();

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
				return;

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
				{
					_presenter.TryQueueMove(index, frame.MoveOptions);
					_lastHoveredMoveIndex = null;
				}
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
		ApplyOutcomeOverlay(frame);
		_hintLabel.Visible = !frame.ShowVictoryOverlay;
		_hintLabel.Text = frame.HintText;
	}

	private void ApplyOutcomeOverlay(PresentationFrame frame) =>
		_outcomeOverlay.SetVisible(frame.ShowVictoryOverlay);

	private void ResetBattle()
	{
		Session.Instance.StartNewRun();
		GetTree().ReloadCurrentScene();
	}

	private void ApplyOrientationHud(PresentationFrame frame)
	{
		if (frame.ShowVictoryOverlay)
		{
			_orientationHud.Show(false);
			return;
		}

		_orientationHud.Show(frame.CanAct && !frame.MissileAimActive);
	}

	private void ApplyUnitViews(PresentationFrame frame)
	{
		foreach (var unit in _presenter.Manager.Units)
		{
			var display = frame.Simulation.Board.StateOf(unit.State.Id);
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

		switch (frame.Mode)
		{
			case EPlayerMode.Move:
				_missileRangeIndicator.SetActive(null, 0);
				_gridView.SetMoveHighlights(
					frame.MoveOptions,
					frame.MovePath,
					frame.MoveTarget,
					frame.PlannedHazardCells);
				break;

			case EPlayerMode.Missile:
				_missileRangeIndicator.SetActive(frame.Simulation.Actor.Position, frame.MissileRange);
				_gridView.SetMissileHighlights(
					frame.PlannedHazardCells,
					frame.ValidMissileCells,
					frame.MissilePreviewCells);
				break;

			case EPlayerMode.Railgun:
				_missileRangeIndicator.SetActive(null, 0);
				_gridView.SetRailgunHighlights(
					frame.RailgunTargetCells,
					frame.RailgunHoveredCell,
					frame.PlannedHazardCells);
				break;
		}
	}

	private void ApplyActionBar(PresentationFrame frame)
	{
		_actionBar.Visible = !frame.ShowVictoryOverlay;
		if (frame.ShowVictoryOverlay)
			return;

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

	public void OnActionApplied(PresentationEvent presentationEvent)
	{
		foreach (var unit in _presenter.Manager.Units)
			_unitViews[unit.State.Id].SyncFromState(unit.State);
	}
}
