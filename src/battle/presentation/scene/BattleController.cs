using Godot;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Camera;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Abilities;
using GrimSpace.Core;
using GrimSpace.Run;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Objectives;
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
	private CombatIntroDirector _combatIntro = null!;

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
		IReadOnlyList<Coord> CommittedPath);

	private bool _introActive;
	private bool _strategicBattle;

	private bool AcceptsCommands =>
		_battle.AcceptsPlayerInput && !_frames.IsInspecting(_battle) && !_introActive;

	public override void _Ready()
	{
		_strategicBattle = RunSession.Instance.Run.ActiveBattle is not null;
		_battle = BattleOrchestrator.FromEncounter(ResolveEncounter());
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
		_battleHud.SetStrategicBattle(_strategicBattle);
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
			(state, color) => _battleView.Ensure(state, color),
			states => _battleView.ApplyUnitStates(states, ColorForActor),
			ApplyReplayState);
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

		_combatIntro = new CombatIntroDirector { Name = "CombatIntroDirector" };
		_combatIntro.Configure(_battle, _battleView, _camera, GetPlayerRenderedPosition);
		AddChild(_combatIntro);

		_agent.PlanningChanged += RefreshPresentation;
		_battle.PhaseChanged += _ => RefreshPresentation();
		_battle.TurnResolved += OnTurnResolved;

		_cameraDirector.EnterManual();
		BeginCombatIntro();
	}

	private void BeginCombatIntro()
	{
		_introActive = true;
		_frames.IntroActive = true;
		var objective = ResolveEncounter().Objective;
		_battleHud.IntroOverlay.SetObjective(objective);
		RefreshPresentation();
		_combatIntro.Play(DismissCombatIntroBanner, EndCombatIntro);
	}

	private void DismissCombatIntroBanner()
	{
		_frames.IntroActive = false;
		RefreshPresentation();
	}

	private void EndCombatIntro()
	{
		_introActive = false;
		_frames.IntroActive = false;
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
		_battleHud.ActionBar.AbilityModeRequested += OnAbilityModeRequested;
		_battleHud.InstructionBar.ConfirmRequested += _translator.OnConfirmAction;
		_battleHud.ActionBar.EndTurnRequested += _translator.OnEndTurn;
		_battleHud.UtilityBar.UndoRequested += _translator.OnUndo;
		_battleHud.UtilityBar.FocusRequested += _translator.OnFocusCamera;
		_battleHud.UtilityBar.BackToPlayerRequested += _translator.OnReturnToPlayer;
		_battleHud.OutcomeOverlay.ResetRequested += OnOutcomeOverlayAction;
		_battleHud.RestartRequested += _translator.OnRestart;
		_battleHud.RetireRequested += _translator.OnRetire;
		_battleHud.MainMenuRequested += GoToMainMenu;
	}

	private void WireTranslator()
	{
		_translator.StagedMountedOnRequested += OnStagedMountedOnRequested;
		_translator.ModeRequested += OnModeRequested;
		_translator.MoveHoverChanged += OnMoveHoverChanged;
		_translator.FlakHoverChanged += mountedOn => SetFlakHoverMountedOn(mountedOn);
		_translator.RailgunHoverChanged += hovered => SetRailgunHovered(hovered);
		_translator.TorpedoHoverChanged += mountedOn => SetTorpedoHoverMountedOn(mountedOn);
		_translator.HoversCleared += ClearHovers;
		_translator.FocusUnitRequested += FocusUnit;
		_translator.ReturnToPlayerRequested += ReturnToPlayer;
		_translator.FocusCameraRequested += () =>
			_cameraDirector.FocusPlayer(GetPlayerRenderedPosition());
		_translator.EndTurnRequested += OnEndTurn;
		_translator.ConfirmationFailed += OnConfirmationFailed;
		_translator.RestartRequested += ResetBattle;
		_translator.RetireRequested += () => _battle.Retire();
	}

	private void OnTurnResolved(TurnReplay replay, int completedTurn)
	{
		_frames.Interaction.ResetAfterTurn();
		_frames.AppendTurn(_battle, completedTurn, replay.History);
		RefreshPresentation();
	}

	private void OnAbilityModeRequested(AbilityHudCatalog.Spec spec)
	{
		if (!AcceptsCommands)
			return;

		_frames.Interaction.SetMode(spec.Mode, spec);
		RefreshPresentation();
	}

	private void OnModeRequested(EPlayerMode mode)
	{
		if (!AcceptsCommands)
			return;

		_frames.Interaction.SetMode(mode);
		RefreshPresentation();
	}

	private void OnStagedMountedOnRequested(ESpatialOrientation mountedOn)
	{
		if (!AcceptsCommands)
			return;

		_frames.Interaction.StageMountedOn(mountedOn);
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
		_moveHoverCache = new MoveHoverCache(frame.MovePaths, frame.CommittedMovePath);
		_translator.SetPresentation(
			enabled: _battle.AcceptsPlayerInput && !_battle.IsBattleOver && !_introActive,
			canIssueActions: frame.CanAct,
			isInspecting: frame.IsInspecting,
			mode: frame.Mode,
			activeAbilitySpec: _frames.Interaction.ActiveAbilitySpec,
			stagedMountedOn: frame.StagedMountedOn,
			moveOptions: frame.MovePaths,
			focusState: frame.FocusState,
			weapons: frame.Weapons,
			instruction: frame.Instruction);
		ApplyFrame(frame);
	}

	private void OnConfirmationFailed()
	{
		_frames.Interaction.ReportConfirmationFailure();
		RefreshPresentation();
	}

	private void ApplyMoveHoverOverlay()
	{
		var (path, target) = MoveUi.GetPathHighlights(
			_moveHoverCache.Paths,
			_frames.Interaction.MoveHoveredIndex,
			_moveHoverCache.CommittedPath);
		_gridView.SetMoveHighlights(_moveHoverCache.Paths, path, target);
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

	private void SetFlakHoverMountedOn(ESpatialOrientation? mountedOn)
	{
		if (!AcceptsCommands || _frames.Interaction.FlakHoverMountedOn == mountedOn)
			return;

		_frames.Interaction.FlakHoverMountedOn = mountedOn;
		RefreshPresentation();
	}

	private void SetRailgunHovered(bool hovered)
	{
		if (!AcceptsCommands || _frames.Interaction.RailgunHovered == hovered)
			return;

		_frames.Interaction.RailgunHovered = hovered;
		RefreshPresentation();
	}

	private void SetTorpedoHoverMountedOn(ESpatialOrientation? mountedOn)
	{
		if (!AcceptsCommands || _frames.Interaction.TorpedoHoverMountedOn == mountedOn)
			return;

		_frames.Interaction.TorpedoHoverMountedOn = mountedOn;
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
		if (ShouldApplyFrameUnitStates(_battle.Phase))
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
		_battleView.ApplyUnitStates(
			states,
			ColorForActor,
			showPredictedDeath: ShouldShowPredictedDeath(_battle.Phase));
		if (_introActive)
			return;

		_battleView.ApplyHitMarks(frame.ThreatenedUnitIds);
	}

	private void ApplyReplayState(ActorState state)
	{
		if (_currentFrame.FocusState.Id == state.Id)
			_battleHud.HealthBar.Set(state);
	}

	internal static bool ShouldApplyFrameUnitStates(EBattlePhase phase) =>
		phase != EBattlePhase.Replaying;

	internal static bool ShouldShowPredictedDeath(EBattlePhase phase) =>
		phase != EBattlePhase.BattleOver;

	private static BattleEncounter ResolveEncounter()
	{
		var activeBattle = RunSession.Instance.Run.ActiveBattle;
		if (activeBattle is not null)
			return activeBattle.Encounter;

		return BattleEncounter.DevDefault(Random.Shared.Next());
	}

	private void OnOutcomeOverlayAction()
	{
		if (_strategicBattle)
			ReturnToStarMap();
		else
			ResetBattle();
	}

	private void ReturnToStarMap()
	{
		RunSession.Instance.ResolveEngagement(_battle.Outcome);
		GetTree().ChangeSceneToFile("res://scenes/map.tscn");
	}

	private void ResetBattle()
	{
		RunSession.Instance.StartNewRun();
		GetTree().ReloadCurrentScene();
	}

	private void GoToMainMenu()
	{
		if (_strategicBattle)
			RunSession.Instance.ResolveEngagement(_battle.Outcome);

		GetTree().ChangeSceneToFile("res://scenes/main.tscn");
	}

	private static Color ColorFor(ETeam team) =>
		team switch
		{
			ETeam.Player => new Color(0.25f, 0.85f, 0.35f),
			ETeam.Enemy => new Color(0.9f, 0.25f, 0.2f),
			_ => Colors.White,
		};

	public override void _ExitTree()
	{
		_battle?.Dispose();
		base._ExitTree();
	}
}
