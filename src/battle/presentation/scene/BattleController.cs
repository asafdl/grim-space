using Godot;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Camera;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Picking;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Units;
using GrimSpace.Core;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Log;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Scene;

/// <summary>
/// Thin Godot adapter: input in, frames and replay playback out. Lifecycle lives in <see cref="BattleDirector"/>.
/// </summary>
public partial class BattleController : Node3D
{
	private BattleUi _ui = null!;
	private BattleDirector _director = null!;
	private TurnReplayPlayer _replayPlayer = null!;
	private BattleView _battleView = null!;
	private BattleHud _battleHud = null!;

	private GridView _gridView = null!;
	private FlakPreviewView _flakPreview = null!;
	private RailgunPreviewView _railgunPreview = null!;
	private TorpedoPreviewView _torpedoPreview = null!;
	private Controller _camera = null!;

	private int? _lastHoveredMoveIndex;
	private PresentationFrame _currentFrame = null!;
	private IReadOnlyDictionary<string, GrimSpace.Battle.Units.State>? _playbackEndStates;
	private MoveHoverCache _moveHoverCache;

	private TurnReplay? _pendingReplay;
	private int _pendingCompletedTurn;
	private int _focusedTurn = -1;

	private readonly record struct MoveHoverCache(
		IReadOnlyList<Movement.MovePathSession> Paths,
		int PathApBaseline,
		IReadOnlyList<Coord> CommittedPath);

	public override void _Ready()
	{
		GameLog.Configure(GD.Print);

		var battle = BattleOrchestrator.FromEncounter(RunSession.Instance.CurrentEncounter);
		_ui = new BattleUi(battle);
		_director = new BattleDirector(_ui);
		_director.FrameChanged += OnFrameChanged;
		_director.ReplayRequested += OnReplayRequested;
		var layout = battle.Layout;

		var backdrop = new SpaceBackdrop();
		backdrop.Build(layout.Grid);
		AddChild(backdrop);
		MoveChild(backdrop, 0);

		_camera = GetNode<Controller>("Camera3D");
		_gridView = GetNode<GridView>("GridView");
		_gridView.Build(layout.Grid);

		_railgunPreview = new RailgunPreviewView { Name = "RailgunPreview" };
		_railgunPreview.Build();
		AddChild(_railgunPreview);

		_flakPreview = new FlakPreviewView { Name = "FlakPreview" };
		_flakPreview.Build();
		AddChild(_flakPreview);

		_torpedoPreview = new TorpedoPreviewView { Name = "TorpedoPreview" };
		_torpedoPreview.Build();
		AddChild(_torpedoPreview);

		var gridCenter = WorldMapping.GridCenter(layout.Grid);
		var playerPosition = battle.Sim.StateOf<ActorState>(battle.PlayerId).Position;
		_camera.SetPivot(WorldMapping.ToWorld(playerPosition));
		var chamberRadius = layout.Grid.Width * WorldMapping.CellSize * 0.5f;
		RedDwarfSun.Configure(GetNode<DirectionalLight3D>("DirectionalLight3D"), gridCenter, chamberRadius);

		var hazardsRoot = new Node3D { Name = "WorldHazards" };
		AddChild(hazardsRoot);
		var hazardView = new BoardHazardView();
		hazardView.Build(layout.TerrainHazards);
		hazardsRoot.AddChild(hazardView);

		var unitsRoot = GetNode<Node3D>("Units");
		_battleView = new BattleView { Name = "BattleView" };
		unitsRoot.AddChild(_battleView);
		_battleView.BindInitial(layout.Participants.Select(pair =>
			(pair.Key, battle.Sim.World.StateOf(pair.Key), ColorFor(pair.Value))));

		_battleHud = new BattleHud { Name = "BattleHud" };
		_battleHud.Build();
		AddChild(_battleHud);
		WireHudEvents();

		_replayPlayer = new TurnReplayPlayer { Name = "TurnReplayPlayer" };
		_replayPlayer.Configure(
			_battleView.UnitViews,
			ColorForActor,
			(state, color) => _battleView.Ensure(state, color));
		_replayPlayer.PlaybackComplete += OnPlaybackComplete;
		AddChild(_replayPlayer);

		_director.Start();
	}

	public override void _Process(double _)
	{
		if (_pendingReplay is TurnReplay replay)
		{
			_pendingReplay = null;
			StartReplay(replay, _pendingCompletedTurn);
		}

		if (!_director.AcceptsInput
			|| _ui.State.Mode != EPlayerMode.Move
			|| _ui.Battle.IsBattleOver
			|| _ui.GetPlanningActor() is null)
		{
			_lastHoveredMoveIndex = null;
			return;
		}

		var index = MovementSelection.PickPathIndex(
			_camera,
			GetViewport().GetMousePosition(),
			_moveHoverCache.Paths);
		if (index == _lastHoveredMoveIndex)
			return;

		_lastHoveredMoveIndex = index;
		_ui.State.SetMoveHover(index, _moveHoverCache.Paths.Count);
		ApplyMoveHoverOverlay();
	}

	private void OnFrameChanged(PresentationFrame frame)
	{
		_currentFrame = frame;
		_moveHoverCache = new MoveHoverCache(
			frame.MovePaths,
			frame.MovePathApBaseline,
			_ui.State.CommittedMovePath);
		ApplyFrame(frame);
	}

	private void OnReplayRequested(TurnReplay replay, int completedTurn)
	{
		_pendingReplay = replay;
		_pendingCompletedTurn = completedTurn;
	}

	private void StartReplay(TurnReplay replay, int completedTurn)
	{
		_playbackEndStates = replay.EndStates;
		_battleView.ApplyUnitStates(replay.StartStates, ColorForActor);
		_replayPlayer.ResetToLive(replay.StartStates, replay.EndStates);
		_replayPlayer.Play(
			replay.History,
			completedTurn,
			_ui.Battle.PlayerId,
			_ui.Battle.OpponentId);
	}

	private void ApplyMoveHoverOverlay()
	{
		var (path, target) = MoveUi.GetPathHighlights(
			_moveHoverCache.Paths,
			_ui.State.MoveHoveredIndex,
			_moveHoverCache.CommittedPath);
		_gridView.SetMoveHighlights(
			_moveHoverCache.Paths,
			_moveHoverCache.PathApBaseline,
			path,
			target);
	}

	private void WireHudEvents()
	{
		_battleHud.OrientationBar.HeadingTurnRequested += turn =>
		{
			if (_director.Enqueue(new HeadingTurnAction(_ui.Battle.PlayerId, turn)))
				_lastHoveredMoveIndex = null;
		};
		_battleHud.OrientationBar.RollRequested += direction =>
		{
			if (_director.Enqueue(new RollAction(_ui.Battle.PlayerId, direction)))
				_lastHoveredMoveIndex = null;
		};
		_battleHud.OutcomeOverlay.ResetRequested += ResetBattle;
		_battleHud.RetireRequested += () => _director.Retire();
		_battleHud.RestartRequested += ResetBattle;
		_battleHud.ActionBar.ModeChanged += mode => _director.SetMode(mode);
		_battleHud.ActionBar.EndTurnRequested += () => _director.EndTurn();
		_battleHud.UtilityBar.FocusRequested += () => FocusCameraOnActiveUnit(_currentFrame);
		_battleHud.UtilityBar.UndoRequested += () =>
		{
			if (_director.Undo())
				_lastHoveredMoveIndex = null;
		};
	}

	public override void _Input(InputEvent @event)
	{
		if (_ui.Battle.IsBattleOver || !_director.AcceptsInput)
			return;

		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Z } key
			&& (key.CtrlPressed || key.MetaPressed)
			&& _battleHud.UtilityBar.TryUndo())
		{
			GetViewport().SetInputAsHandled();
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_ui.Battle.IsBattleOver)
			return;

		if (!_director.AcceptsInput)
		{
			if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
				PresentationDiagnostics.LogInputIgnored(_director.Phase, "left_click");

			if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Space })
				PresentationDiagnostics.LogInputIgnored(_director.Phase, "end_turn");

			return;
		}

		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape }
			&& _ui.State.Mode is EPlayerMode.Flak or EPlayerMode.Railgun or EPlayerMode.Torpedo)
		{
			_director.SetMode(EPlayerMode.Move);
			GetViewport().SetInputAsHandled();
			return;
		}

		if (@event is InputEventKey { Pressed: true, Echo: false } abilityKey
			&& AbilityHotkeySlot(abilityKey.Keycode) is int slot
			&& _battleHud.ActionBar.TryActivateHotkey(slot))
		{
			GetViewport().SetInputAsHandled();
			return;
		}

		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Q }
			&& _battleHud.OrientationBar.TrySpin())
		{
			GetViewport().SetInputAsHandled();
			return;
		}

		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.E }
			&& _battleHud.OrientationBar.TryYaw())
		{
			GetViewport().SetInputAsHandled();
			return;
		}

		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Space })
		{
			_director.EndTurn();
			GetViewport().SetInputAsHandled();
			return;
		}

		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.F }
			&& _battleHud.UtilityBar.TryFocus())
		{
			GetViewport().SetInputAsHandled();
			return;
		}

		var frame = _currentFrame;
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
		switch (_ui.State.Mode)
		{
			case EPlayerMode.Move:
				return;

			case EPlayerMode.Flak:
				_director.SetFlakHover(
					GridPick.PickFromSet(_camera, screenPos, frame.ValidFlakPickCells));
				break;

			case EPlayerMode.Railgun:
				_director.SetRailgunHover(
					GridPick.PickFromSet(_camera, screenPos, frame.RailgunCells));
				break;

			case EPlayerMode.Torpedo:
				_director.SetTorpedoHover(
					GridPick.PickFromSet(_camera, screenPos, frame.TorpedoMountCells));
				break;
		}
	}

	private void HandleLeftClick(Vector2 screenPos, PresentationFrame frame)
	{
		switch (_ui.State.Mode)
		{
			case EPlayerMode.Move:
				if (MovementSelection.PickPathIndex(_camera, screenPos, frame.MovePaths) is int index)
				{
					_director.QueueMove(frame.MovePaths[index].EndPosition);
					_lastHoveredMoveIndex = null;
				}
				else
					PresentationDiagnostics.LogMovePickMiss(frame.MovePaths.Count);
				break;

			case EPlayerMode.Flak:
				if (GridPick.PickFromSet(_camera, screenPos, frame.ValidFlakPickCells) is { } flakCell)
					_director.ApplyFlak(flakCell);
				break;

			case EPlayerMode.Railgun:
				if (GridPick.PickFromSet(_camera, screenPos, frame.RailgunCells) is { } railgunCell)
					_director.ApplyRailgun(railgunCell);
				break;

			case EPlayerMode.Torpedo:
				if (GridPick.PickFromSet(_camera, screenPos, frame.TorpedoMountCells) is { } torpedoCell)
					_director.ApplyTorpedo(torpedoCell);
				break;
		}
	}

	private void OnPlaybackComplete()
	{
		if (_playbackEndStates is not null)
			_battleView.ApplyUnitStates(_playbackEndStates, ColorForActor);

		_playbackEndStates = null;
		_director.NotifyReplayComplete();
	}

	private Color ColorForActor(string actorId)
	{
		if (UnitRegistry.For(_ui.Battle.Sim.World).TryGet(actorId, out var unit))
			return ColorFor(unit.Alliance.Team);

		if (_ui.Battle.Layout.Participants.TryGetValue(actorId, out var team))
			return ColorFor(team);

		return Colors.White;
	}

	private void ApplyFrame(PresentationFrame frame)
	{
		ApplyUnitStates(frame);
		_gridView.ApplyFrame(frame);
		_flakPreview.ApplyFrame(frame);
		_railgunPreview.ApplyFrame(frame);
		_torpedoPreview.ApplyFrame(frame);
		_battleHud.Apply(frame);

		if (!_director.AcceptsInput || frame.ActiveUnit is null)
			return;

		var turn = _ui.Battle.TurnNumber;
		if (turn == _focusedTurn)
			return;

		_focusedTurn = turn;
		FocusCameraOnActiveUnit(frame);
	}

	private void FocusCameraOnActiveUnit(PresentationFrame frame)
	{
		if (frame.ActiveUnit is null)
			return;

		_camera.FocusOn(WorldMapping.ToWorld(frame.ActorState.Position));
	}

	private void ApplyUnitStates(PresentationFrame frame)
	{
		var states = UnitRegistry.For(frame.PreviewWorld).All.ToDictionary(
			unit => unit.State.Id,
			unit => unit.State);
		_battleView.ApplyUnitStates(states, id => ColorForPreview(frame, id));
		_battleView.ApplyHitMarks(frame.ThreatenedUnitIds);
	}

	private Color ColorForPreview(PresentationFrame frame, string actorId) =>
		UnitRegistry.For(frame.PreviewWorld).TryGet(actorId, out var unit)
			? ColorFor(unit.Alliance.Team)
			: ColorForActor(actorId);

	private void ResetBattle()
	{
		RunSession.Instance.StartNewRun();
		GetTree().ReloadCurrentScene();
	}

	private static Color ColorFor(ETeam team) =>
		team switch
		{
			ETeam.Player => new Color(0.25f, 0.85f, 0.35f),
			ETeam.Enemy => new Color(0.9f, 0.25f, 0.2f),
			_ => Colors.White,
		};

	private static int? AbilityHotkeySlot(Key keycode) =>
		keycode switch
		{
			Key.Key1 => 1,
			Key.Key2 => 2,
			Key.Key3 => 3,
			Key.Key4 => 4,
			_ => null,
		};
}
