using Godot;
using GrimSpace.Battle.Presentation.Camera;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Picking;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core;
using GrimSpace.Core.Log;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Scene;

/// <summary>
/// Thin scene connector: wires Godot input and nodes to <see cref="BattleUi"/> and <see cref="BattleOrchestrator"/>.
/// </summary>
public partial class BattleController : Node3D
{
	private BattleUi _ui = null!;
	private TurnReplayPlayer _replayPlayer = null!;

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
		GameLog.Configure(GD.Print);

		var battle = BattleOrchestrator.FromEncounter(RunSession.Instance.CurrentEncounter);
		_ui = new BattleUi(battle);
		var layout = battle.Layout;

		var backdrop = new SpaceBackdrop();
		backdrop.Build(layout.Grid);
		AddChild(backdrop);
		MoveChild(backdrop, 0);

		_camera = GetNode<Controller>("Camera3D");
		_gridView = GetNode<GridView>("GridView");
		_gridView.Build(layout.Grid);

		var gridCenter = WorldMapping.GridCenter(layout.Grid);
		_camera.SetPivot(gridCenter);
		var chamberRadius = layout.Grid.Width * WorldMapping.CellSize * 0.5f;
		RedDwarfSun.Configure(GetNode<DirectionalLight3D>("DirectionalLight3D"), gridCenter, chamberRadius);

		var hazardsRoot = new Node3D { Name = "WorldHazards" };
		AddChild(hazardsRoot);
		var hazardView = new BoardHazardView();
		hazardView.Build(layout.TerrainHazards);
		hazardsRoot.AddChild(hazardView);

		_missileRangeIndicator = new MissileRangeIndicator();
		AddChild(_missileRangeIndicator);

		var unitsRoot = GetNode<Node3D>("Units");
		foreach (var (unitId, controller) in layout.Participants)
		{
			var view = new UnitView();
			view.Bind(battle.Sim.World.StateOf(unitId), ColorFor(controller));
			unitsRoot.AddChild(view);
			_unitViews[unitId] = view;
		}

		SetupHintLabel();
		SetupActionBar();
		SetupOutcomeOverlay();
		SetupOrientationHud();

		_replayPlayer = new TurnReplayPlayer { Name = "TurnReplayPlayer" };
		_replayPlayer.Configure(_unitViews, ColorForActor);
		_replayPlayer.PlaybackComplete += OnPlaybackComplete;
		AddChild(_replayPlayer);

		Refresh();
	}

	public override void _Process(double _)
	{
		if (_ui.Battle.IsResolving
			|| _replayPlayer.IsPlaying
			|| _ui.Mode != EPlayerMode.Move
			|| _ui.Battle.IsBattleOver
			|| _ui.Battle.GetActiveActor() is null)
		{
			_lastHoveredMoveIndex = null;
			return;
		}

		var frame = _ui.BuildFrame();
		var index = MovementSelection.PickOptionIndex(_camera, GetViewport().GetMousePosition(), frame.MoveOptions);
		if (index == _lastHoveredMoveIndex)
			return;

		_lastHoveredMoveIndex = index;
		_ui.SetMoveHover(index, frame.MoveOptions.Count);
		Refresh();
	}

	private void SetupOrientationHud()
	{
		_orientationHud = new ShipOrientationHud();
		_orientationHud.HeadingTurnRequested += turn =>
		{
			if (_ui.TryQueueHeadingTurn(turn))
			{
				_lastHoveredMoveIndex = null;
				Refresh();
			}
		};
		_orientationHud.RollRequested += direction =>
		{
			if (_ui.TryQueueRoll(direction))
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
		_actionBar.ModeChanged += mode =>
		{
			if (mode == EPlayerMode.Flak)
				_ui.SelectFlakMode();
			else
				_ui.SetMode(mode);

			ExitAimIfNeeded();
			Refresh();
		};
		_actionBar.MissileMountSelected += mount => { _ui.SelectMissileMount(mount); Refresh(); };
		_actionBar.EndTurnRequested += TryEndTurn;
		AddChild(_actionBar);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_ui.Battle.IsBattleOver)
			return;

		if (_ui.Battle.IsResolving || _replayPlayer.IsPlaying)
			return;

		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
		{
			if (_ui.Mode == EPlayerMode.Missile)
			{
				_ui.CancelMissileMode();
				_camera.ExitAim();
				Refresh();
			}
			else if (_ui.Mode == EPlayerMode.Flak)
			{
				_ui.CancelFlakMode();
				Refresh();
			}

			GetViewport().SetInputAsHandled();
			return;
		}

		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Z } key
			&& (key.CtrlPressed || key.MetaPressed))
		{
			if (_ui.Undo())
			{
				_lastHoveredMoveIndex = null;
				Refresh();
			}

			GetViewport().SetInputAsHandled();
			return;
		}

		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Space })
		{
			TryEndTurn();
			GetViewport().SetInputAsHandled();
			return;
		}

		if (_ui.Mode == EPlayerMode.Missile
			&& @event is InputEventMouseButton { Pressed: true } scroll
			&& scroll.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
		{
			var delta = scroll.ButtonIndex == MouseButton.WheelUp ? 1 : -1;
			if (_ui.AdjustMissileRange(delta))
				Refresh();

			GetViewport().SetInputAsHandled();
			return;
		}

		var frame = _ui.BuildFrame();
		if (_ui.Battle.IsBattleOver || frame.ActiveUnit is null)
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
		switch (_ui.Mode)
		{
			case EPlayerMode.Move:
				return;

			case EPlayerMode.Missile:
				_ui.SetMissileHover(
					GridPick.PickFromSet(_camera, screenPos, frame.ValidMissileCells));
				break;

			case EPlayerMode.Flak:
				_ui.SetFlakHover(
					GridPick.PickFromSet(_camera, screenPos, frame.ValidFlakPickCells));
				break;

			case EPlayerMode.Railgun:
				var picked = GridPick.PickUnit(_camera, screenPos, _ui.Battle.Sim.World.Units.Values.ToList());
				_ui.SetRailgunHover(picked);
				break;
		}

		Refresh();
	}

	private void HandleLeftClick(Vector2 screenPos, PresentationFrame frame)
	{
		switch (_ui.Mode)
		{
			case EPlayerMode.Move:
				if (MovementSelection.PickOptionIndex(_camera, screenPos, frame.MoveOptions) is int index)
				{
					_ui.TryQueueMove(index, frame.MoveOptions);
					_lastHoveredMoveIndex = null;
				}
				break;

			case EPlayerMode.Missile:
				if (GridPick.PickFromSet(_camera, screenPos, frame.ValidMissileCells) is { } center)
					_ui.TryQueueMissile(center);
				break;

			case EPlayerMode.Flak:
				if (GridPick.PickFromSet(_camera, screenPos, frame.ValidFlakPickCells) is { } flakCell)
					_ui.TryQueueFlak(flakCell);
				break;

			case EPlayerMode.Railgun:
				var target = GridPick.PickUnit(_camera, screenPos, _ui.Battle.Sim.World.Units.Values.ToList());
				if (target is not null)
					_ui.TryQueueRailgun(target);
				break;
		}

		Refresh();
	}

	private void Refresh()
	{
		if (_replayPlayer.IsPlaying)
			return;

		var frame = _ui.BuildFrame();
		ApplyFrame(frame);
	}

	private void TryEndTurn()
	{
		if (_ui.Battle.IsBattleOver || _ui.Battle.IsResolving || _replayPlayer.IsPlaying)
			return;

		_replayPlayer.PrepareTurnStart(_ui.Battle.LiveUnitStates);
		var applied = _ui.CommitAndResolve();
		if (applied is null)
			return;

		_replayPlayer.Play(applied);
	}

	private void OnPlaybackComplete()
	{
		ExitAimIfNeeded();
		Refresh();
	}

	private Color ColorForActor(string actorId) =>
		_ui.Battle.Layout.Participants.TryGetValue(actorId, out var controller)
			? ColorFor(controller)
			: Colors.White;

	private void ApplyFrame(PresentationFrame frame)
	{
		ApplyUnitViews(frame);
		ApplyCamera(frame);
		ApplyGrid(frame);
		ApplyActionBar(frame);
		ApplyOrientationHud(frame);
		ApplyOutcomeOverlay(frame);
		_hintLabel.Visible = !frame.ShowOutcomeOverlay;
		_hintLabel.Text = frame.HintText;
	}

	private void ApplyOutcomeOverlay(PresentationFrame frame)
	{
		_outcomeOverlay.SetVisible(frame.ShowOutcomeOverlay);
		if (frame.ShowOutcomeOverlay)
			_outcomeOverlay.SetOutcome(frame.PlayerWon);
	}

	private void ResetBattle()
	{
		RunSession.Instance.StartNewRun();
		GetTree().ReloadCurrentScene();
	}

	private void ApplyOrientationHud(PresentationFrame frame)
	{
		if (frame.ShowOutcomeOverlay)
		{
			_orientationHud.Show(false);
			return;
		}

		_orientationHud.Show(frame.CanAct && !frame.MissileAimActive && frame.Mode != EPlayerMode.Flak);
	}

	private void ApplyUnitViews(PresentationFrame frame)
	{
		foreach (var unitId in _ui.Battle.Layout.Participants.Keys)
		{
			var display = frame.PreviewWorld.StateOf(unitId);
			_unitViews[unitId].SyncFromState(display);
		}
	}

	private void ApplyCamera(PresentationFrame frame)
	{
		if (frame.MissileAimActive && frame.MissileAimShip is not null)
			_camera.EnterForeAim(frame.MissileAimShip);
		else if (frame.ExitMissileMode)
			_camera.ExitAim();
	}

	private void ApplyGrid(PresentationFrame frame)
	{
		if (frame.ActiveUnit is null || _ui.Battle.IsBattleOver)
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
					frame.PreviewHazardCells);
				break;

			case EPlayerMode.Missile:
				_missileRangeIndicator.SetActive(frame.ActorState.Position, frame.MissileRange);
				_gridView.SetMissileHighlights(
					frame.PreviewHazardCells,
					frame.ValidMissileCells,
					frame.MissilePreviewCells);
				break;

			case EPlayerMode.Flak:
				_missileRangeIndicator.SetActive(null, 0);
				_gridView.SetFlakHighlights(
					frame.PreviewHazardCells,
					frame.ValidFlakPortCells,
					frame.ValidFlakStarboardCells,
					frame.FlakPreviewCells);
				break;

			case EPlayerMode.Railgun:
				_missileRangeIndicator.SetActive(null, 0);
				_gridView.SetRailgunHighlights(
					frame.RailgunTargetCells,
					frame.RailgunHoveredCell,
					frame.PreviewHazardCells);
				break;
		}
	}

	private void ApplyActionBar(PresentationFrame frame)
	{
		_actionBar.Visible = !frame.ShowOutcomeOverlay;
		if (frame.ShowOutcomeOverlay)
			return;

		_actionBar.SetMode(frame.Mode, frame.MissileMount);
		_actionBar.Configure(
			frame.MissilesRemaining,
			CombatConfig.MissilesPerTurn,
			frame.FlakAvailable,
			frame.CanAct);
	}

	private void ExitAimIfNeeded()
	{
		if (_ui.Mode != EPlayerMode.Missile)
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
