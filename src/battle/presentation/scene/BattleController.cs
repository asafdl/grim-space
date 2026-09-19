using Godot;
using GrimSpace.Application;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Camera;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Abilities;
using GrimSpace.Run;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Objectives;
using GrimSpace.Components;
using GrimSpace.Education;
using GrimSpace.Tutorials;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Scene;

/// <summary>
/// Scene coordinator: owns presentation frame + interaction state, wires world views, HUD, and replay.
/// </summary>
/// 
/// TODO: _battle and _agents are signs of leaks, shouldn't be here
public partial class BattleController : Node3D
{
	private const int TutorialDialogWidth = 380;
	private const int TutorialDialogTop = 120;
	private const double OpeningCameraHoldSeconds = 1.0;

	private BattleOrchestrator _battle = null!;
	private UserExecutionAgent _agent = null!;
	private PresentationFrameBuilder _frames = null!;
	private UserIntentTranslator _translator = null!;
	private ReplayDirector _replayDirector = null!;
	private TurnReplayPlayer _replayPlayer = null!;
	private BattleView _battleView = null!;
	private BattleHud _battleHud = null!;
	private TutorialController? _tutorial;
	private Node3D _unitsRoot = null!;

	private GridView _gridView = null!;
	private AreaActionPreviewView _areaActionPreview = null!;
	private TorpedoPreviewView _torpedoPreview = null!;
	private AbilitySourcePickerView _abilitySourcePicker = null!;
	private CellVolumeMeshStore _cellVolumeMeshes = null!;
	private Controller _camera = null!;
	private BattleCameraDirector _cameraDirector = null!;
	private MoveGhostView _moveGhost = null!;
	private TargetOpportunityOverlay _targetOpportunityOverlay = null!;

	private PresentationFrame _currentFrame = null!;

	private bool _strategicBattle;

	private bool AcceptsCommands =>
		_battle.AcceptsPlayerInput && !_frames.IsInspecting(_battle);
	private bool CanEndTurn =>
		ShouldAllowEndTurn(
			AcceptsCommands,
			_tutorial?.IsActive == true && !_tutorial.AllowsEndTurn);

	public override void _Ready()
	{
		_strategicBattle = Session.Instance.Run.ActiveBattle is not null;
		var encounter = ResolveEncounter();
		_battle = Session.Instance.CreateBattleOrchestrator(encounter);
		Session.Instance.DevMenu.SetBattleActions(
			() => _battle.CanForceOutcome,
			() => ForceOutcome(EBattleResult.Win),
			() => ForceOutcome(EBattleResult.Lose));
		_agent = _battle.PlayerAgent;
		_frames = new PresentationFrameBuilder();
		var layout = _battle.Layout;

		var backdrop = new SpaceBackdrop();
		backdrop.Build(layout.Grid, encounter.Seed);
		AddChild(backdrop);
		MoveChild(backdrop, 0);

		_camera = GetNode<Controller>("Camera3D");
		_cameraDirector = new BattleCameraDirector(_camera);
		_camera.ManualInputStarted += _cameraDirector.OnManualInputStarted;
		_cellVolumeMeshes = new CellVolumeMeshStore();
		_gridView = GetNode<GridView>("GridView");
		_gridView.Build(_camera, _cellVolumeMeshes);

		_areaActionPreview = new AreaActionPreviewView { Name = "AreaActionPreview" };
		_areaActionPreview.Build(_cellVolumeMeshes);
		AddChild(_areaActionPreview);

		_torpedoPreview = new TorpedoPreviewView { Name = "TorpedoPreview" };
		_torpedoPreview.Build(_cellVolumeMeshes);
		AddChild(_torpedoPreview);

		_abilitySourcePicker = new AbilitySourcePickerView { Name = "AbilitySourcePicker" };
		_abilitySourcePicker.Configure(_camera);
		AddChild(_abilitySourcePicker);

		var gridCenter = WorldMapping.GridCenter(layout.Grid);
		var openingPose = BattleCameraPoses.PlayerAft(
			_agent.Sim.StateOf<ActorState>(_battle.PlayerId));
		_camera.SetFocus(
			openingPose.Pivot,
			openingPose.Distance,
			openingPose.Yaw,
			openingPose.Pitch);
		var chamberRadius = layout.Grid.Width * WorldMapping.CellSize * 0.5f;
		RedDwarfSun.Configure(GetNode<DirectionalLight3D>("DirectionalLight3D"), gridCenter, chamberRadius);

		var hazardsRoot = new Node3D { Name = "WorldHazards" };
		AddChild(hazardsRoot);
		var hazardView = new BoardHazardView();
		hazardView.Build(layout.TerrainHazards);
		hazardsRoot.AddChild(hazardView);

		_unitsRoot = GetNode<Node3D>("Units");
		_battleView = new BattleView { Name = "BattleView" };
		_unitsRoot.AddChild(_battleView);
		_battleView.BindInitial(layout.Participants.Select(pair =>
			(pair.Key, _agent.Sim.World.StateOf(pair.Key), ColorFor(pair.Value))));
		_moveGhost = new MoveGhostView { Name = "MoveGhost" };
		_moveGhost.Configure(_camera);
		_unitsRoot.AddChild(_moveGhost);

		var opportunityLayer = new CanvasLayer
		{
			Name = "TargetOpportunityLayer",
			Layer = 20,
		};
		_targetOpportunityOverlay = new TargetOpportunityOverlay { Name = "TargetOpportunityOverlay" };
		_targetOpportunityOverlay.Configure(_camera);
		opportunityLayer.AddChild(_targetOpportunityOverlay);
		AddChild(opportunityLayer);

		_battleHud = new BattleHud { Name = "BattleHud" };
		_battleHud.Build();
		_battleHud.SetStrategicBattle(_strategicBattle);
		AddChild(_battleHud);

		_translator = new UserIntentTranslator(
			_battle.PlayerId,
			_agent,
			_camera,
			_battleHud,
			_abilitySourcePicker,
			() => _battleView.UnitViews)
		{
			Name = "UserIntentTranslator",
		};
		AddChild(_translator);
		_camera.ManualInputStarted += _translator.OnCameraManualInputStarted;
		WireTranslator();
		WireHudToTranslator();

		_replayPlayer = new TurnReplayPlayer { Name = "TurnReplayPlayer" };
		_replayPlayer.Configure(
			_battleView.UnitViews,
			ColorForActor,
			(state, color) => _battleView.Ensure(state, color),
			_battleView.Remove,
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

		_agent.PlanningChanged += RefreshPresentation;
		_battle.PhaseChanged += OnPhaseChanged;
		_battle.TurnResolved += OnTurnResolved;

		_cameraDirector.EnterManual();
		RefreshPresentation();
		GetTree().CreateTimer(OpeningCameraHoldSeconds).Timeout += ConfigureTutorial;
	}

	private void ConfigureTutorial()
	{
		if (!IsInsideTree())
			return;
		if (!GameSettings.ReadShowTutorials())
			return;

		var worldIndicators = new BattleWorldIndicators { Name = "BattleWorldIndicators" };
		worldIndicators.Configure(_battle.Layout, _battleView);
		AddChild(worldIndicators);

		var tutorialLayer = new CanvasLayer
		{
			Name = "TutorialLayer",
			Layer = 20,
		};
		var dialog = new TutorialDialog();
		dialog.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
		dialog.OffsetLeft = HudStyles.Margin;
		dialog.OffsetTop = TutorialDialogTop;
		dialog.OffsetRight = TutorialDialogWidth + HudStyles.Margin;
		dialog.OffsetBottom = TutorialDialogTop;
		tutorialLayer.AddChild(dialog);
		AddChild(tutorialLayer);

		_tutorial = TutorialController.CreateForBattle(
			_battle,
			Session.Instance.Run.TutorialProgress,
			dialog,
			new WorldLinkNavigator(
				new BattleWorldFocus(
					_camera,
					_battle.Layout,
					_battleView,
					() => _agent.Sim.StateOf<ActorState>(_battle.PlayerId)),
				worldIndicators),
			CreatePosedUnitGhost("TutorialGhost"));
		_tutorial.Completed += OnTutorialCompleted;
		RefreshPresentation();
	}

	public PosedUnitGhostView CreatePosedUnitGhost(string name)
	{
		ArgumentException.ThrowIfNullOrEmpty(name);
		var ghost = new PosedUnitGhostView { Name = name };
		_unitsRoot.AddChild(ghost);
		return ghost;
	}

	public override void _Process(double delta)
	{
		if (_cameraDirector.NeedsTick)
			_cameraDirector.Tick((float)delta, GetPlayerRenderedPosition());

		var meshUpdates = _cellVolumeMeshes.Pump(_currentFrame.SimulationTick);
		foreach (var failure in meshUpdates.Failures)
			GD.PushError($"Cell-volume mesh generation failed for {failure.Key}: {failure.Error}");
		if (meshUpdates.ProcessedCount > 0)
			ApplyFrame(_currentFrame);
	}

	private void WireHudToTranslator()
	{
		_battleHud.ManeuverBar.MoveModeRequested += _translator.OnMoveMode;
		_battleHud.ActionBar.AbilityModeRequested += OnAbilityModeRequested;
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
		_translator.ModeRequested += OnModeRequested;
		_translator.MoveHoverChanged += OnMoveHoverChanged;
		_translator.MoveSelectionStarted += OnMoveSelectionStarted;
		_translator.MoveReopenRequested += OnMoveReopenRequested;
		_translator.MovePoseRequested += basis =>
		{
			_frames.Interaction.SetMovePose(basis);
			RefreshPresentation();
		};
		_translator.MoveSelectionCanceled += () =>
		{
			_frames.Interaction.ClearMoveSelection();
			_tutorial?.NotifyMoveSelectionCanceled();
			RefreshPresentation();
		};
		_translator.MoveSelectionCompleted += () =>
		{
			_frames.Interaction.CompleteMoveSelection();
			RefreshPresentation();
		};
		_translator.AbilityHoverChanged += OnAbilityHoverChanged;
		_translator.FocusUnitRequested += FocusUnit;
		_translator.ReturnToPlayerRequested += ReturnToPlayer;
		_translator.FocusCameraRequested += () =>
			_cameraDirector.FocusPlayer(GetPlayerRenderedPosition());
		_translator.UndoRequested += OnUndoRequested;
		_translator.UndoShortcutRequested += OnUndoShortcutRequested;
		_translator.EndTurnRequested += OnEndTurn;
		_translator.ActionFailed += OnActionFailed;
		_translator.RestartRequested += ResetBattle;
		_translator.RetireRequested += () => _battle.Retire();
	}

	private void OnPhaseChanged(EBattlePhase phase)
	{
		RefreshPresentation();
		_tutorial?.NotifyBattlePhaseChanged(phase);
	}

	private void OnTurnResolved(TurnReplay replay, int completedTurn)
	{
		_frames.Interaction.ResetAfterTurn();
		_frames.AppendTurn(_battle, completedTurn, replay.History);
		_tutorial?.NotifyBattleTurnResolved(completedTurn);
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

	private void OnMoveSelectionStarted(Coord destination, GridBasis basis)
	{
		_frames.Interaction.BeginMoveSelection(destination, basis);
		_tutorial?.NotifyMoveSelectionStarted();
		RefreshPresentation();
	}

	private void OnMoveReopenRequested(Coord clickedCell)
	{
		var freshFrame = _frames.BuildFrame(_battle, _agent, AcceptsCommands);
		if (freshFrame.ReopenMoveCell != clickedCell)
			return;

		if (!TryUndo())
			return;

		var option = MovementSelection.ResolveOption(_currentFrame.MovePaths, clickedCell);
		if (option is null)
		{
			_frames.Interaction.ReportActionFailure();
			RefreshPresentation();
			return;
		}

		OnMoveSelectionStarted(option.EndPosition, option.EndBasis);
	}

	private void OnMoveHoverChanged(Coord? cell)
	{
		_frames.Interaction.SetMoveHover(cell, _currentFrame.MovePaths);
		RefreshPresentation();
	}

	private void OnEndTurn()
	{
		if (!CanEndTurn)
			return;

		_battle.EndTurn();
		RefreshPresentation();
	}

	private void OnUndoRequested()
	{
		TryUndo();
	}

	private void OnUndoShortcutRequested() => TryUndo();

	private bool TryUndo()
	{
		_frames.Interaction.ClearHovers();
		_frames.Interaction.ClearPoseHitOpportunities();
		var undone = _agent.Undo();
		if (!undone)
			RefreshPresentation();
		return undone;
	}

	private void RefreshPresentation()
	{
		var frame = _frames.BuildFrame(_battle, _agent, AcceptsCommands);
		_currentFrame = frame;
		_translator.SetPresentation(
			enabled: _battle.AcceptsPlayerInput && !_battle.IsBattleOver,
			canIssueActions: frame.CanAct,
			isInspecting: frame.IsInspecting,
			mode: frame.Mode,
			moveOptions: frame.MovePaths,
			hoveredMove: frame.HoveredMove,
			moveHoveredCell: _frames.Interaction.MoveHoveredCell,
			selectedMove: frame.SelectedMove,
			moveDestination: frame.MoveDestination,
			moveDragging: frame.IsMoveDragging,
			abilityChoices: frame.AbilityChoices,
			abilityHoveredIndex: frame.AbilityHoveredIndex,
			reopenMoveCell: frame.ReopenMoveCell);
		ApplyFrame(frame);
	}

	private void OnActionFailed()
	{
		_frames.Interaction.ReportActionFailure();
		RefreshPresentation();
	}

	private void OnAbilityHoverChanged(int? index, int optionCount)
	{
		if (!AcceptsCommands || _frames.Interaction.AbilityHoveredIndex == index)
			return;

		_frames.Interaction.SetAbilityHover(index, optionCount);
		RefreshPresentation();
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

	private Color ColorForActor(string actorId)
	{
		if (UnitRegistry.For(_battle.Engine.World).TryGet(actorId, out var unit))
			return ColorFor(unit.Team);

		if (_battle.Layout.Participants.TryGetValue(actorId, out var team))
			return ColorFor(team);

		return Colors.White;
	}

	private void ApplyFrame(PresentationFrame frame)
	{
		if (ShouldApplyFrameUnitStates(_battle.Phase))
			ApplyUnitStates(frame);
		_gridView.ApplyFrame(frame);
		_moveGhost.Apply(
			frame.MoveGhostState,
			frame.MoveCheckpoints,
			frame.FocusState,
			frame.ReachableMoveHeadings,
			ColorForActor(frame.FocusId),
			selected: frame.SelectedMove is not null);
		_targetOpportunityOverlay.Apply(frame.PreviewUnits, frame.PoseHitOpportunities);
		_abilitySourcePicker.Apply(frame.AbilityChoices, frame.AbilityHoveredIndex);
		_areaActionPreview.ApplyFrame(frame);
		_torpedoPreview.ApplyFrame(frame);
		_battleHud.Apply(frame, allowEndTurn: CanEndTurn);
	}

	private void OnTutorialCompleted(TutorialFlow _) => RefreshPresentation();

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

	internal static bool ShouldAllowEndTurn(bool acceptsCommands, bool tutorialBlocksEndTurn) =>
		acceptsCommands && !tutorialBlocksEndTurn;

	private static BattleEncounter ResolveEncounter()
	{
		var activeBattle = Session.Instance.Run.ActiveBattle;
		if (activeBattle is not null)
			return activeBattle;

		return BattleEncounter.DevDefault(Random.Shared.Next());
	}

	private void OnOutcomeOverlayAction()
	{
		if (_strategicBattle)
			ReturnToStarMap();
		else
			ResetBattle();
	}

	private void ForceOutcome(EBattleResult result)
	{
		_battle.ForceOutcome(result);
	}

	private void ReturnToStarMap()
	{
		if (!_battle.IsBattleOver)
			return;

		if (Session.Instance.Run.ActiveBattle is not null)
			return;

		GetTree().ChangeSceneToFile("res://scenes/map.tscn");
	}

	private void ResetBattle()
	{
		Session.Instance.StartNewRun();
		GetTree().ReloadCurrentScene();
	}

	private void GoToMainMenu()
	{
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
		_camera.ManualInputStarted -= _cameraDirector.OnManualInputStarted;
		_camera.ManualInputStarted -= _translator.OnCameraManualInputStarted;
		if (_tutorial is not null)
		{
			_tutorial.Completed -= OnTutorialCompleted;
			_tutorial.Dispose();
		}
		Session.Instance.DevMenu.ClearBattleActions();
		Session.Instance.ReleaseBattleOutcomeSubscription();
		_cellVolumeMeshes?.Dispose();
		_battle?.Dispose();
		base._ExitTree();
	}
}
