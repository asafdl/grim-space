using Godot;
using GrimSpace.Battle.Presentation.Camera;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Picking;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core;
using GrimSpace.Core.Log;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Scene;

/// <summary>
/// Thin scene connector: wires Godot input and nodes to <see cref="BattleUi"/> and <see cref="BattleOrchestrator"/>.
/// </summary>
public partial class BattleController : Node3D
{
	private enum BattleMode
	{
		Planning,
		Playback,
	}

	private BattleUi _ui = null!;
	private TurnReplayPlayer _replayPlayer = null!;
	private BattleView _battleView = null!;
	private PreviewOverlay _previewOverlay = null!;
	private BattleHud _battleHud = null!;

	private GridView _gridView = null!;
	private Controller _camera = null!;
	private MissileRangeIndicator _missileRangeIndicator = null!;

	private int? _lastHoveredMoveIndex;
	private IReadOnlyDictionary<string, GrimSpace.Battle.Units.State>? _playbackEndStates;
	private MoveHoverCache _moveHoverCache;

	private readonly record struct MoveHoverCache(
		IReadOnlyList<Movement.Option> Options,
		IReadOnlySet<Coord> HazardCells,
		IReadOnlyList<Coord> CommittedPath);

	private BattleMode Mode =>
		_replayPlayer.IsPlaying || _ui.Battle.IsResolving ? BattleMode.Playback : BattleMode.Planning;

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
		_battleView = new BattleView { Name = "BattleView" };
		unitsRoot.AddChild(_battleView);
		_battleView.BindInitial(layout.Participants.Select(pair =>
			(pair.Key, battle.Sim.World.StateOf(pair.Key), ColorFor(pair.Value))));

		_previewOverlay = new PreviewOverlay { Name = "PreviewOverlay" };
		_previewOverlay.Configure(_gridView, _missileRangeIndicator);
		AddChild(_previewOverlay);

		_battleHud = new BattleHud { Name = "BattleHud" };
		_battleHud.Build();
		AddChild(_battleHud);
		WireHudEvents();

		_replayPlayer = new TurnReplayPlayer { Name = "TurnReplayPlayer" };
		_replayPlayer.Configure(_battleView.UnitViews, ColorForActor);
		_replayPlayer.PlaybackComplete += OnPlaybackComplete;
		AddChild(_replayPlayer);

		Refresh();
	}

	public override void _Process(double _)
	{
		if (Mode != BattleMode.Planning
			|| _ui.Mode != EPlayerMode.Move
			|| _ui.Battle.IsBattleOver
			|| _ui.Battle.GetActiveActor() is null)
		{
			_lastHoveredMoveIndex = null;
			return;
		}

		var index = MovementSelection.PickOptionIndex(
			_camera,
			GetViewport().GetMousePosition(),
			_moveHoverCache.Options);
		if (index == _lastHoveredMoveIndex)
			return;

		_lastHoveredMoveIndex = index;
		_ui.SetMoveHover(index, _moveHoverCache.Options.Count);
		ApplyMoveHoverOverlay();
	}

	private void ApplyMoveHoverOverlay()
	{
		var (path, target) = MoveUi.GetPathHighlights(
			_moveHoverCache.Options,
			_ui.MoveHoveredIndex,
			_moveHoverCache.CommittedPath);
		_previewOverlay.ApplyMoveHover(
			_moveHoverCache.Options,
			path,
			target,
			_moveHoverCache.HazardCells);
	}

	private void WireHudEvents()
	{
		_battleHud.OrientationHud.HeadingTurnRequested += turn =>
		{
			if (_ui.TryQueueHeadingTurn(turn))
			{
				_lastHoveredMoveIndex = null;
				Refresh();
			}
		};
		_battleHud.OrientationHud.RollRequested += direction =>
		{
			if (_ui.TryQueueRoll(direction))
			{
				_lastHoveredMoveIndex = null;
				Refresh();
			}
		};
		_battleHud.OutcomeOverlay.ResetRequested += ResetBattle;
		_battleHud.ActionBar.ModeChanged += mode =>
		{
			if (mode == EPlayerMode.Flak)
				_ui.SelectFlakMode();
			else
				_ui.SetMode(mode);

			ExitAimIfNeeded();
			Refresh();
		};
		_battleHud.ActionBar.MissileMountSelected += mount => { _ui.SelectMissileMount(mount); Refresh(); };
		_battleHud.ActionBar.EndTurnRequested += TryEndTurn;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_ui.Battle.IsBattleOver)
			return;

		if (Mode == BattleMode.Playback)
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
		_moveHoverCache = new MoveHoverCache(
			frame.MoveOptions,
			frame.PreviewHazardCells,
			_ui.CommittedMovePath);
		ApplyFrame(frame);
	}

	private void TryEndTurn()
	{
		if (_ui.Battle.IsBattleOver || Mode == BattleMode.Playback)
			return;

		var replay = _ui.CommitAndResolve();
		if (replay is null)
			return;

		_playbackEndStates = replay.EndStates;
		_battleView.ApplyUnitStates(replay.StartStates);
		_replayPlayer.ResetToLive(replay.StartStates);
		_replayPlayer.Play(replay.AppliedActions);
	}

	private void OnPlaybackComplete()
	{
		if (_playbackEndStates is not null)
			_battleView.ApplyUnitStates(_playbackEndStates);

		_playbackEndStates = null;
		ExitAimIfNeeded();
		Refresh();
	}

	private Color ColorForActor(string actorId) =>
		_ui.Battle.Layout.Participants.TryGetValue(actorId, out var controller)
			? ColorFor(controller)
			: Colors.White;

	private void ApplyFrame(PresentationFrame frame)
	{
		ApplyUnitStates(frame);
		_previewOverlay.Apply(frame);
		_battleHud.Apply(frame);
		ApplyCamera(frame);
	}

	private void ApplyUnitStates(PresentationFrame frame)
	{
		var states = new Dictionary<string, GrimSpace.Battle.Units.State>();
		foreach (var unitId in _ui.Battle.Layout.Participants.Keys)
			states[unitId] = frame.PreviewWorld.StateOf(unitId);

		_battleView.ApplyUnitStates(states);
	}

	private void ResetBattle()
	{
		RunSession.Instance.StartNewRun();
		GetTree().ReloadCurrentScene();
	}

	private void ApplyCamera(PresentationFrame frame)
	{
		if (frame.MissileAimActive && frame.MissileAimShip is not null)
			_camera.EnterForeAim(frame.MissileAimShip);
		else if (frame.ExitMissileMode)
			_camera.ExitAim();
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
}
