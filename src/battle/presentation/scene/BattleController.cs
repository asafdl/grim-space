using Godot;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Camera;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Scene;

/// <summary>
/// Scene coordinator: owns presentation frame + interaction state, wires world views, HUD, and replay.
/// </summary>
/// 
/// TODO: _battle and _agents are signs of leaks, shouldn't be here
public partial class BattleController : Node3D
{
	private BattleOrchestrator _battle = null!;
	private UserExecutionAgent _agent = null!;
	private PresentationFrameBuilder _frames = null!;
	private UserIntentTranslator _translator = null!;
	private ReplayDirector _replayDirector = null!;
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
	private MoveHoverCache _moveHoverCache;

	private readonly record struct MoveHoverCache(
		IReadOnlyList<MovePathOption> Paths,
		int PathApBaseline,
		IReadOnlyList<Coord> CommittedPath);

	private bool AcceptsCommands =>
		_battle.AcceptsPlayerInput && !_frames.IsInspecting(_battle);

	public override void _Ready()
	{
		_battle = BattleOrchestrator.FromEncounter(RunSession.Instance.CurrentEncounter);
		_agent = _battle.PlayerAgent;
		_frames = new PresentationFrameBuilder();
		var layout = _battle.Layout;

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
		var playerPosition = _agent.Sim.StateOf<ActorState>(_battle.PlayerId).Position;
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
			(pair.Key, _agent.Sim.World.StateOf(pair.Key), ColorFor(pair.Value))));

		_battleHud = new BattleHud { Name = "BattleHud" };
		_battleHud.Build();
		AddChild(_battleHud);

		_translator = new UserIntentTranslator(
			_battle.PlayerId,
			_agent,
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
		AddChild(_replayPlayer);

		_replayDirector = new ReplayDirector { Name = "ReplayDirector" };
		_replayDirector.Configure(
			_battle,
			_replayPlayer,
			_battleView,
			_cameraDirector,
			GetPlayerRenderedPosition,
			ColorForActor);
		AddChild(_replayDirector);

		_agent.PlanningChanged += RefreshPresentation;
		_battle.PhaseChanged += _ => RefreshPresentation();
		_battle.TurnResolved += OnTurnResolved;

		_cameraDirector.EnterManual();
		RefreshPresentation();
	}

	public override void _Process(double delta)
	{
		if (_cameraDirector.NeedsTick)
			_cameraDirector.Tick((float)delta, GetPlayerRenderedPosition());
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
		_translator.FlakHoverChanged += mount => SetFlakHoverMount(mount);
		_translator.RailgunHoverChanged += hovered => SetRailgunHovered(hovered);
		_translator.TorpedoHoverChanged += mount => SetTorpedoHoverMount(mount);
		_translator.HoversCleared += ClearHovers;
		_translator.FocusUnitRequested += FocusUnit;
		_translator.ReturnToPlayerRequested += ReturnToPlayer;
		_translator.FocusCameraRequested += () =>
			_cameraDirector.FocusPlayer(GetPlayerRenderedPosition());
		_translator.EndTurnRequested += OnEndTurn;
		_translator.RestartRequested += ResetBattle;
		_translator.RetireRequested += () => _battle.Retire();
	}

	private void OnTurnResolved(TurnReplay replay, int completedTurn)
	{
		_frames.Interaction.ResetAfterTurn();
		_frames.AppendTurn(_battle, completedTurn, replay.History);
		RefreshPresentation();
	}

	private void OnModeRequested(EPlayerMode mode)
	{
		if (!AcceptsCommands)
			return;

		_frames.Interaction.SetMode(mode);
		RefreshPresentation();
	}

	private void OnMoveHoverChanged(int? index, int optionCount)
	{
		_frames.Interaction.SetMoveHover(index, optionCount);
		ApplyMoveHoverOverlay();
	}

	private void OnEndTurn()
	{
		if (!AcceptsCommands)
			return;

		_battle.EndTurn();
		RefreshPresentation();
	}

	private void RefreshPresentation()
	{
		var frame = _frames.BuildFrame(_battle, _agent, AcceptsCommands);
		_currentFrame = frame;
		_moveHoverCache = new MoveHoverCache(
			frame.MovePaths,
			frame.MovePathApBaseline,
			frame.CommittedMovePath);
		_translator.SetPresentation(
			enabled: _battle.AcceptsPlayerInput && !_battle.IsBattleOver,
			canIssueActions: frame.CanAct,
			isInspecting: frame.IsInspecting,
			mode: frame.Mode,
			moveOptions: frame.MovePaths,
			focusState: frame.FocusState);
		ApplyFrame(frame);
	}

	private void ApplyMoveHoverOverlay()
	{
		var (path, target) = MoveUi.GetPathHighlights(
			_moveHoverCache.Paths,
			_frames.Interaction.MoveHoveredIndex,
			_moveHoverCache.CommittedPath);
		_gridView.SetMoveHighlights(
			_moveHoverCache.Paths,
			_moveHoverCache.PathApBaseline,
			path,
			target);
	}

	private void FocusUnit(string unitId)
	{
		if (!_battle.AcceptsPlayerInput)
			return;

		var previewUnits = _frames.BuildFrame(_battle, _agent, acceptsCommands: false).PreviewUnits;
		if (!previewUnits.TryGetValue(unitId, out var unit) || !unit.IsAlive)
			return;

		_frames.Interaction.FocusUnit(unitId);
		RefreshPresentation();
	}

	private void ReturnToPlayer()
	{
		if (!_battle.AcceptsPlayerInput)
			return;

		_frames.Interaction.ClearFocus();
		RefreshPresentation();
		_cameraDirector.FocusPlayer(GetPlayerRenderedPosition());
	}

	private void ClearHovers()
	{
		_frames.Interaction.ClearHovers();
		RefreshPresentation();
	}

	private void SetFlakHoverMount(EFlakMount? mount)
	{
		if (!AcceptsCommands || _frames.Interaction.FlakHoverMount == mount)
			return;

		_frames.Interaction.FlakHoverMount = mount;
		RefreshPresentation();
	}

	private void SetRailgunHovered(bool hovered)
	{
		if (!AcceptsCommands || _frames.Interaction.RailgunHovered == hovered)
			return;

		_frames.Interaction.RailgunHovered = hovered;
		RefreshPresentation();
	}

	private void SetTorpedoHoverMount(ETorpedoMount? mount)
	{
		if (!AcceptsCommands || _frames.Interaction.TorpedoHoverMount == mount)
			return;

		_frames.Interaction.TorpedoHoverMount = mount;
		RefreshPresentation();
	}

	private Color ColorForActor(string actorId)
	{
		if (UnitRegistry.For(_battle.Engine.World).TryGet(actorId, out var unit))
			return ColorFor(unit.Alliance.Team);

		if (_battle.Layout.Participants.TryGetValue(actorId, out var team))
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

	private Vector3 GetPlayerRenderedPosition()
	{
		var playerId = _battle.PlayerId;
		if (_battleView.UnitViews.TryGetValue(playerId, out var view))
			return view.GlobalPosition;

		return WorldMapping.ToWorld(_agent.Sim.StateOf<ActorState>(playerId).Position);
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
		if (_battle.Layout.Participants.TryGetValue(actorId, out var team))
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
