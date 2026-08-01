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
	private Controller _camera = null!;

	private int? _lastHoveredMoveIndex;
	private PresentationFrame _currentFrame = null!;
	private IReadOnlyDictionary<string, GrimSpace.Battle.Units.State>? _playbackEndStates;
	private MoveHoverCache _moveHoverCache;

	private TurnReplay? _pendingReplay;
	private int _pendingCompletedTurn;

	private readonly record struct MoveHoverCache(
		IReadOnlyList<Movement.Option> Options,
		IReadOnlySet<Coord> HazardCells,
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
		_replayPlayer.Configure(_battleView.UnitViews, ColorForActor);
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

		var index = MovementSelection.PickOptionIndex(
			_camera,
			GetViewport().GetMousePosition(),
			_moveHoverCache.Options);
		if (index == _lastHoveredMoveIndex)
			return;

		_lastHoveredMoveIndex = index;
		_ui.State.SetMoveHover(index, _moveHoverCache.Options.Count);
		ApplyMoveHoverOverlay();
	}

	private void OnFrameChanged(PresentationFrame frame)
	{
		_currentFrame = frame;
		_moveHoverCache = new MoveHoverCache(
			frame.MoveOptions,
			frame.PreviewHazardCells,
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
		_battleView.ApplyUnitStates(replay.StartStates);
		_replayPlayer.ResetToLive(replay.StartStates);
		_replayPlayer.Play(
			replay.AppliedActions,
			completedTurn,
			_ui.Battle.PlayerId,
			_ui.Battle.OpponentId);
	}

	private void ApplyMoveHoverOverlay()
	{
		var (path, target) = MoveUi.GetPathHighlights(
			_moveHoverCache.Options,
			_ui.State.MoveHoveredIndex,
			_moveHoverCache.CommittedPath);
		_gridView.SetMoveHighlights(
			_moveHoverCache.Options,
			path,
			target,
			_moveHoverCache.HazardCells);
	}

	private void WireHudEvents()
	{
		_battleHud.OrientationHud.HeadingTurnRequested += turn =>
		{
			if (_director.Enqueue(new HeadingTurnAction(_ui.Battle.PlayerId, turn)))
				_lastHoveredMoveIndex = null;
		};
		_battleHud.OrientationHud.RollRequested += direction =>
		{
			if (_director.Enqueue(new RollAction(_ui.Battle.PlayerId, direction)))
				_lastHoveredMoveIndex = null;
		};
		_battleHud.OutcomeOverlay.ResetRequested += ResetBattle;
		_battleHud.ActionBar.ModeChanged += mode => _director.SetMode(mode);
		_battleHud.ActionBar.EndTurnRequested += () => _director.EndTurn();
	}

	public override void _Input(InputEvent @event)
	{
		if (_ui.Battle.IsBattleOver || !_director.AcceptsInput)
			return;

		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Z } key
			&& (key.CtrlPressed || key.MetaPressed))
		{
			if (_director.Undo())
				_lastHoveredMoveIndex = null;

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
			&& _ui.State.Mode is EPlayerMode.Flak or EPlayerMode.Railgun)
		{
			_director.SetMode(EPlayerMode.Move);
			GetViewport().SetInputAsHandled();
			return;
		}

		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Space })
		{
			_director.EndTurn();
			GetViewport().SetInputAsHandled();
			return;
		}

		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.R })
		{
			_camera.ResetView();
			GetViewport().SetInputAsHandled();
			return;
		}

		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.F })
		{
			var player = _ui.Battle.Sim.StateOf<ActorState>(_ui.Battle.PlayerId);
			_camera.FocusOn(WorldMapping.ToWorld(player.Position));
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
		}
	}

	private void HandleLeftClick(Vector2 screenPos, PresentationFrame frame)
	{
		switch (_ui.State.Mode)
		{
			case EPlayerMode.Move:
				if (MovementSelection.PickOptionIndex(_camera, screenPos, frame.MoveOptions) is int index)
				{
					_director.QueueMove(frame.MoveOptions[index].EndPosition);
					_lastHoveredMoveIndex = null;
				}
				else
					PresentationDiagnostics.LogMovePickMiss(frame.MoveOptions.Count);
				break;

			case EPlayerMode.Flak:
				if (GridPick.PickFromSet(_camera, screenPos, frame.ValidFlakPickCells) is { } flakCell)
					_director.ApplyFlak(flakCell);
				break;

			case EPlayerMode.Railgun:
				if (GridPick.PickFromSet(_camera, screenPos, frame.RailgunCells) is { } railgunCell)
					_director.ApplyRailgun(railgunCell);
				break;
		}
	}

	private void OnPlaybackComplete()
	{
		if (_playbackEndStates is not null)
			_battleView.ApplyUnitStates(_playbackEndStates);

		_playbackEndStates = null;
		_director.NotifyReplayComplete();
	}

	private Color ColorForActor(string actorId) =>
		_ui.Battle.Layout.Participants.TryGetValue(actorId, out var controller)
			? ColorFor(controller)
			: Colors.White;

	private void ApplyFrame(PresentationFrame frame)
	{
		ApplyUnitStates(frame);
		_gridView.ApplyFrame(frame);
		_battleHud.Apply(frame);
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

	private static Color ColorFor(EController controller) =>
		controller switch
		{
			EController.Player => new Color(0.25f, 0.85f, 0.35f),
			EController.Enemy => new Color(0.9f, 0.25f, 0.2f),
			_ => Colors.White,
		};
}
