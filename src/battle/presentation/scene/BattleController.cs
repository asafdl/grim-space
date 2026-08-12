using Godot;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Camera;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Units;
using GrimSpace.Core;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Scene;

/// <summary>
/// Scene coordinator: wires HUD/views, applies published frames, owns presentation interaction state.
/// User intents are mapped by <see cref="UserIntentTranslator"/>.
/// </summary>
public partial class BattleController : Node3D
{
	private BattleUi _ui = null!;
	private BattleDirector _director = null!;
	private UserIntentTranslator _translator = null!;
	private TurnReplayPlayer _replayPlayer = null!;
	private BattleView _battleView = null!;
	private BattleHud _battleHud = null!;

	private GridView _gridView = null!;
	private FlakPreviewView _flakPreview = null!;
	private RailgunPreviewView _railgunPreview = null!;
	private TorpedoPreviewView _torpedoPreview = null!;
	private Controller _camera = null!;
	private BattleCameraDirector _cameraDirector = null!;

	private PresentationFrame _currentFrame = null!;
	private IReadOnlyDictionary<string, GrimSpace.Battle.Units.State>? _playbackEndStates;
	private MoveHoverCache _moveHoverCache;

	private TurnReplay? _pendingReplay;
	private int _pendingCompletedTurn;
	private PresentationPhase? _lastPhase;

	private readonly record struct MoveHoverCache(
		IReadOnlyList<MovePathOption> Paths,
		int PathApBaseline,
		IReadOnlyList<Coord> CommittedPath);

	public override void _Ready()
	{
		var battle = BattleOrchestrator.FromEncounter(RunSession.Instance.CurrentEncounter);
		var agent = battle.PlayerAgent;
		_ui = new BattleUi(battle, agent);
		_director = new BattleDirector(_ui, agent);
		_director.FrameChanged += OnFrameChanged;
		_director.ReplayRequested += OnReplayRequested;
		var layout = battle.Layout;

		var backdrop = new SpaceBackdrop();
		backdrop.Build(layout.Grid);
		AddChild(backdrop);
		MoveChild(backdrop, 0);

		_camera = GetNode<Controller>("Camera3D");
		_cameraDirector = new BattleCameraDirector(_camera);
		_camera.ManualInputStarted += _cameraDirector.OnManualInputStarted;
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

		_translator = new UserIntentTranslator(
			battle.PlayerId,
			agent,
			_camera,
			_battleHud,
			_flakPreview,
			_railgunPreview,
			() => _battleView.UnitViews)
		{
			Name = "UserIntentTranslator",
		};
		AddChild(_translator);
		WireTranslator();
		WireHudToTranslator();

		_replayPlayer = new TurnReplayPlayer { Name = "TurnReplayPlayer" };
		_replayPlayer.Configure(
			_battleView.UnitViews,
			ColorForActor,
			(state, color) => _battleView.Ensure(state, color));
		_replayPlayer.PlaybackComplete += OnPlaybackComplete;
		AddChild(_replayPlayer);

		_director.Start();
	}

	public override void _Process(double delta)
	{
		if (_cameraDirector.NeedsTick)
			_cameraDirector.Tick((float)delta, GetPlayerRenderedPosition());

		if (_pendingReplay is TurnReplay replay)
		{
			_pendingReplay = null;
			StartReplay(replay, _pendingCompletedTurn);
		}
	}

	private void WireHudToTranslator()
	{
		_battleHud.ManeuverBar.YawRequested += _translator.OnYaw;
		_battleHud.ManeuverBar.SpinRequested += _translator.OnSpin;
		_battleHud.ManeuverBar.MoveModeRequested += _translator.OnMoveMode;
		_battleHud.ActionBar.FlakModeRequested += _translator.OnFlakMode;
		_battleHud.ActionBar.RailgunModeRequested += _translator.OnRailgunMode;
		_battleHud.ActionBar.TorpedoModeRequested += _translator.OnTorpedoMode;
		_battleHud.ActionBar.EndTurnRequested += _translator.OnEndTurn;
		_battleHud.UtilityBar.UndoRequested += _translator.OnUndo;
		_battleHud.UtilityBar.FocusRequested += _translator.OnFocusCamera;
		_battleHud.UtilityBar.BackToPlayerRequested += _translator.OnReturnToPlayer;
		_battleHud.OutcomeOverlay.ResetRequested += _translator.OnRestart;
		_battleHud.RestartRequested += _translator.OnRestart;
		_battleHud.RetireRequested += _translator.OnRetire;
	}

	private void WireTranslator()
	{
		_translator.ModeRequested += OnModeRequested;
		_translator.MoveHoverChanged += OnMoveHoverChanged;
		_translator.FlakHoverChanged += mount => _director.SetFlakHoverMount(mount);
		_translator.RailgunHoverChanged += hovered => _director.SetRailgunHovered(hovered);
		_translator.TorpedoHoverChanged += mount => _director.SetTorpedoHoverMount(mount);
		_translator.HoversCleared += () => _director.ClearHovers();
		_translator.FocusUnitRequested += unitId => _director.FocusUnit(unitId);
		_translator.ReturnToPlayerRequested += ReturnToPlayer;
		_translator.FocusCameraRequested += () =>
			_cameraDirector.FocusPlayer(GetPlayerRenderedPosition());
		_translator.EndTurnRequested += () => _director.EndTurn();
		_translator.RestartRequested += ResetBattle;
		_translator.RetireRequested += () => _director.Retire();
	}

	private void OnModeRequested(EPlayerMode mode)
	{
		if (!_director.AcceptsCommands)
			return;

		_ui.State.SetMode(mode);
		_director.RefreshFrame();
	}

	private void OnMoveHoverChanged(int? index, int optionCount)
	{
		_ui.State.SetMoveHover(index, optionCount);
		ApplyMoveHoverOverlay();
	}

	private void OnFrameChanged(PresentationFrame frame)
	{
		_currentFrame = frame;
		_moveHoverCache = new MoveHoverCache(
			frame.MovePaths,
			frame.MovePathApBaseline,
			frame.CommittedMovePath);
		_translator.SetPresentation(
			enabled: _director.AcceptsInput && !_ui.Battle.IsBattleOver,
			canIssueActions: frame.CanAct,
			isInspecting: frame.IsInspecting,
			mode: frame.Mode,
			moveOptions: frame.MovePaths,
			focusState: frame.FocusState);
		ApplyFrame(frame);
		HandleCameraPhaseTransition();
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
		_replayPlayer.ResetToLive(
			replay.StartStates,
			replay.EndStates,
			interest => _cameraDirector.ReportInterest(interest));
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

	private void ReturnToPlayer()
	{
		if (!_director.ClearFocus())
			return;

		_cameraDirector.FocusPlayer(GetPlayerRenderedPosition());
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
	}

	private void HandleCameraPhaseTransition()
	{
		var phase = _director.Phase;
		if (phase == _lastPhase)
			return;

		var previous = _lastPhase;
		_lastPhase = phase;

		switch (phase)
		{
			case PresentationPhase.Planning when previous == PresentationPhase.Replaying:
				_cameraDirector.ReturnControl(GetPlayerRenderedPosition());
				break;

			case PresentationPhase.Planning when previous is null:
				_cameraDirector.EnterManual();
				break;

			case PresentationPhase.Replaying:
				_cameraDirector.BeginPlayback();
				break;
		}
	}

	private Vector3 GetPlayerRenderedPosition()
	{
		var playerId = _ui.Battle.PlayerId;
		if (_battleView.UnitViews.TryGetValue(playerId, out var view))
			return view.GlobalPosition;

		return WorldMapping.ToWorld(_ui.Battle.Sim.StateOf<ActorState>(playerId).Position);
	}

	private void ApplyUnitStates(PresentationFrame frame)
	{
		var states = frame.PreviewUnits.ToDictionary(
			entry => entry.Key,
			entry => entry.Value.ToState());
		_battleView.ApplyUnitStates(states, id => ColorForPreview(frame, id));
		_battleView.ApplyHitMarks(frame.ThreatenedUnitIds);
	}

	private Color ColorForPreview(PresentationFrame frame, string actorId)
	{
		if (_ui.Battle.Layout.Participants.TryGetValue(actorId, out var team))
			return ColorFor(team);

		return Colors.White;
	}

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
}
