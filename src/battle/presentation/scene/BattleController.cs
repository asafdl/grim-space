using System.Diagnostics;
using Godot;
using GrimSpace.Application;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Player;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Camera;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.NonUnits;
using GrimSpace.Run;
using GrimSpace.Run.Persistence;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Objectives;
using GrimSpace.Components;
using GrimSpace.Education;
using GrimSpace.Presentation.Support;
using GrimSpace.Tutorials;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Presentation.Scene;

namespace GrimSpace.Battle.Presentation.Scene;

/// <summary>
/// Scene coordinator: owns presentation frame + interaction state, wires world views, HUD, and replay.
/// </summary>
/// 
/// TODO: _battle and _agents are signs of leaks, shouldn't be here
public partial class BattleController : Node3D
{
	public static BattleEncounter? NextEncounterOverride { get; set; }
	public BattleOrchestrator Battle => _battle;
	private const double OpeningCameraHoldSeconds = 1.0;

	private BattleOrchestrator _battle = null!;
	private UserExecutionAgent? _agent = null;
	private PresentationFrameBuilder _frames = null!;
	private UserIntentTranslator _translator = null!;
	private ReplayDirector _replayDirector = null!;
	private TurnReplayPlayer _replayPlayer = null!;
	private BattleView _battleView = null!;
	private BoardAsteroidView _asteroidView = null!;
	private EnemyRangeTetherView _enemyRangeTether = null!;
	private GoopHazardView _goopHazardView = null!;
	private BattleHud _battleHud = null!;
	private BattleTutorialAdapter? _battleTutorial;
	private Node3D _unitsRoot = null!;

	private GridView _gridView = null!;
	private AreaActionPreviewView _areaActionPreview = null!;
	private VoidBombPreviewView _voidBombPreview = null!;
	private AbilitySourcePickerView _abilitySourcePicker = null!;
	private CellVolumeMeshStore _cellVolumeMeshes = null!;
	private Controller _camera = null!;
	private BattleCameraDirector _cameraDirector = null!;
	private MoveGhostView _moveGhost = null!;
	private TargetOpportunityOverlay _targetOpportunityOverlay = null!;

	private PresentationFrame _currentFrame = null!;

	private bool _strategicBattle;
	private EBattlePhase _lastPhase = (EBattlePhase)(-1);
	private readonly List<IReadOnlyList<string>> _completedTurnOrders = [];
	private readonly List<string> _resolvedRoundActivations = [];
	private readonly Dictionary<string, EType> _unitTypes = new(StringComparer.Ordinal);
	private IReadOnlyList<string> _currentTurnActivations = [];
	private IReadOnlyList<TurnFlowEntry> _turnFlowTimeline = [];
	private bool _replayCompletesRound;
	private bool AcceptsCommands =>
		_battle.AcceptsPlayerInput && !_frames.IsInspecting(_battle);
	private bool CanEndTurn =>
		ShouldAllowEndTurn(
			AcceptsCommands,
			_battleTutorial is not null
			&& Session.Instance.Run.Tutorials?.IsActive == true
			&& !_battleTutorial.AllowsEndTurn);

	public override void _Ready()
	{
		_strategicBattle = Session.Instance.Run?.ActiveBattle is not null;
		int backdropSeed;
		if (NextEncounterOverride is { } encounterOverride)
		{
			NextEncounterOverride = null;
			_strategicBattle = false;
			_battle = BattleOrchestrator.FromEncounter(encounterOverride);
			backdropSeed = encounterOverride.Seed;
			CachePilotIdsFromEncounter(encounterOverride);
		}
		else if (_strategicBattle)
		{
			try
			{
				_battle = Session.Instance.Run.CreateActiveBattleOrchestrator();
				backdropSeed = Session.Instance.Run.ActiveBattle!.Seed;
			}
			catch (InvalidOperationException)
			{
				Session.Instance.Run.AbandonUnplayableActiveBattle();
				Callable.From(() =>
					GetTree().ChangeSceneToFile(MapNavigationContext.MapScenePath)).CallDeferred();
				return;
			}
		}
		else
		{
			var devEncounter = BattleEncounter.DevDefault(Random.Shared.Next());
			_battle = BattleOrchestrator.FromEncounter(devEncounter);
			backdropSeed = devEncounter.Seed;
		}
		Session.Instance.RegisterAutosaveContext(
			() => _battle.Engine.World,
			() => new SaveGateState(
				HasActiveRun: true,
				StrategicWaitingForPlayerInput: false,
				StrategicResolvingAction: false,
				StrategicHasUncommittedPlayerBatch: false,
				BattleResolving: _battle.Phase == EBattlePhase.Resolving,
				BattleReplaying: _battle.Phase == EBattlePhase.Replaying,
				SceneTransitioning: false,
				BattleTurnInProgress:
					_battle.Phase == EBattlePhase.PlayerTurn
					&& !_battle.IsAtRoundStart));
		Session.Instance.DevMenu.SetBattleActions(
			() => _battle.CanForceOutcome,
			() => ForceOutcome(EBattleResult.Win),
			() => ForceOutcome(EBattleResult.Lose));
		_agent = _battle.PlayerAgent as UserExecutionAgent;
		_frames = new PresentationFrameBuilder();
		var layout = _battle.Layout;

		var backdrop = new SpaceBackdrop();
		backdrop.Build(layout.Grid, backdropSeed);
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

		_voidBombPreview = new VoidBombPreviewView { Name = "VoidBombPreview" };
		_voidBombPreview.Build(_cellVolumeMeshes);
		AddChild(_voidBombPreview);

		_abilitySourcePicker = new AbilitySourcePickerView { Name = "AbilitySourcePicker" };
		_abilitySourcePicker.Configure(_camera);
		AddChild(_abilitySourcePicker);

		var gridCenter = WorldMapping.GridCenter(layout.Grid);
		var openingPose = BattleCameraPoses.PlayerAft(
			_battle.Engine.World.StateOf(_battle.PlayerId));
		_camera.SetFocus(
			openingPose.Pivot,
			openingPose.Distance,
			openingPose.Yaw,
			openingPose.Pitch);
		var chamberRadius = layout.Grid.Width * WorldMapping.CellSize * 0.5f;
		RedDwarfSun.Configure(GetNode<DirectionalLight3D>("DirectionalLight3D"), gridCenter, chamberRadius);

		var hazardsRoot = new Node3D { Name = "WorldHazards" };
		AddChild(hazardsRoot);
		_asteroidView = new BoardAsteroidView();
		_asteroidView.Build(_camera, layout.Asteroids);
		hazardsRoot.AddChild(_asteroidView);
		_goopHazardView = new GoopHazardView { Name = "GoopHazards" };
		hazardsRoot.AddChild(_goopHazardView);
		SyncAuthoritativeHazardViews();

		_unitsRoot = GetNode<Node3D>("Units");
		_battleView = new BattleView { Name = "BattleView" };
		_unitsRoot.AddChild(_battleView);
		_battleView.BindInitial(layout.Participants.Select(pair =>
			(pair.Key, _battle.Engine.World.StateOf(pair.Key), ColorFor(pair.Value))));
		_enemyRangeTether = new EnemyRangeTetherView { Name = "EnemyRangeTether" };
		_enemyRangeTether.Configure(layout.Grid);
		_unitsRoot.AddChild(_enemyRangeTether);
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
		_battleHud.TurnFlowBar.SetPortraitResolver(PortraitForUnit);
		AddChild(_battleHud);

		_translator = new UserIntentTranslator(
			_battle.PlayerId,
			_agent as IActionSink,
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
			ApplyReplayState,
			_goopHazardView.Show,
			_goopHazardView.Remove);
		_replayPlayer.TurnFlowIndexChanged += OnTurnFlowIndexChanged;
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

		var ue = _agent as UserExecutionAgent;
		ue?.PlanningChanged += RefreshPresentation;
		_battle.PhaseChanged += OnPhaseChanged;
		_battle.TurnResolved += OnTurnResolved;

		_cameraDirector.EnterManual();
		SyncAuthoritativeUnitViewsFromWorld();
		RefreshPresentation();
		GetTree().CreateTimer(OpeningCameraHoldSeconds).Timeout += ConfigureTutorial;
	}

	private void ConfigureTutorial()
	{
		if (!IsInsideTree())
			return;
		if (_agent == null)
			return;

		var run = Session.Instance.Run;
		if (run.Tutorials is null && !GameSettings.ReadShowTutorials())
			return;
		if (run.Tutorials is null)
			run.ConfigureTutorials(true);
		if (run.Tutorials is not { } tutorials)
			return;

		var worldIndicators = new BattleWorldIndicators { Name = "BattleWorldIndicators" };
		worldIndicators.Configure(_battle.Layout, _battleView);
		AddChild(worldIndicators);

		var host = Session.Instance.TutorialDialogHost;
		host.ApplyBattleLayout();
		_battleTutorial = new BattleTutorialAdapter(
			tutorials,
			_battle,
			CreatePosedUnitGhost("TutorialGhost"),
			() => _cameraDirector.FocusPlayer(GetPlayerRenderedPosition()));
		_battleTutorial.Attach(
			host.Dialog,
			new BattleWorldFocus(
				_camera,
				_battle.Layout,
				_battleView,
				() => _agent?.Sim.StateOf<ActorState>(_battle.PlayerId) ?? default
			),
			worldIndicators
		);
		tutorials.FlowCompleted += OnTutorialFlowCompleted;
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
		if (_cellVolumeMeshes is null || _currentFrame is null)
			return;

		if (_cameraDirector.NeedsTick)
			_cameraDirector.Tick((float)delta, GetPlayerRenderedPosition());

		if (_battleHud.IsMountSelectorOpen)
			ApplyMountSelector(_currentFrame);
		UpdateAsteroidOcclusionTargets(_currentFrame);
		var meshUpdates = _cellVolumeMeshes.Pump(_currentFrame.SimulationTick);
		foreach (var failure in meshUpdates.Failures)
			GD.PushError($"Cell-volume mesh generation failed for {failure.Key}: {failure.Error}");
		if (meshUpdates.ProcessedCount > 0)
			ApplyFrame(_currentFrame);
	}

	private void WireHudToTranslator()
	{
		_battleHud.TurnFlowBar.UnitClicked += FocusUnitFromTopBar;
		_battleHud.CommandHud.AbilityModeRequested += OnAbilityModeRequested;
		_battleHud.MountFaceHovered += _translator.OnMountFaceHovered;
		_battleHud.MountFaceSelected += _translator.OnMountFaceSelected;
		_battleHud.MountSelectionCanceled += _translator.OnMountSelectionCanceled;
		_battleHud.CommandHud.ExecuteRequested += _translator.OnEndTurn;
		_battleHud.CommandHud.UndoRequested += _translator.OnUndo;
		_battleHud.CommandHud.FocusRequested += _translator.OnFocusCamera;
		_battleHud.CommandHud.CycleTargetRequested += CycleEnemyFocus;
		_battleHud.CommandHud.ReturnToPlayerRequested += _translator.OnReturnToPlayer;
		_battleHud.OutcomeOverlay.ResetRequested += OnOutcomeOverlayAction;
		_battleHud.RestartRequested += _translator.OnRestart;
		_battleHud.RetireRequested += _translator.OnRetire;
		_battleHud.MainMenuRequested += GoToMainMenu;
		_battleHud.PauseMenu.SaveRequested += SaveGame;
	}

	private void SaveGame()
	{
		if (_battle.Phase != EBattlePhase.PlayerTurn || _replayPlayer.IsPlaying)
			return;

		var result = Session.Instance.TrySaveGame(_battle.Engine.World);
		if (result == GrimSpace.Run.Persistence.SaveStorageResult.Success)
			_battleHud.PauseMenu.ShowSaveConfirmation();
		else
			GD.PrintErr($"Failed to save game: {result}");
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
			_battleTutorial?.NotifyMoveSelectionCanceled();
			RefreshPresentation();
		};
		_translator.MoveSelectionCompleted += () =>
		{
			_frames.Interaction.CompleteMoveSelection();
			RefreshPresentation();
		};
		_translator.AbilityHoverChanged += OnAbilityHoverChanged;
		_translator.FocusUnitRequested += FocusUnit;
		_translator.CycleEnemyRequested += CycleEnemyFocus;
		_translator.ReturnToPlayerRequested += ReturnToPlayer;
		_translator.FocusCameraRequested += () =>
			_cameraDirector.FocusPlayer(GetPlayerRenderedPosition());
		_translator.UndoRequested += OnUndoRequested;
		_translator.UndoShortcutRequested += OnUndoShortcutRequested;
		_translator.EndTurnRequested += OnEndTurn;
		_translator.ActionFailed += OnActionFailed;
		_translator.AbilityConfirmed += OnAbilityConfirmed;
		_translator.RestartRequested += ResetBattle;
		_translator.RetireRequested += () => _battle.Retire();
	}

	private void OnPhaseChanged(EBattlePhase phase)
	{
		var previous = _lastPhase;
		_lastPhase = phase;
		if (phase == EBattlePhase.PlayerTurn)
		{
			BindActivePlayer();
			SyncAuthoritativeUnitViewsFromWorld();
			SyncAuthoritativeHazardViews();
		}
		if (phase == EBattlePhase.PlayerTurn && previous == EBattlePhase.Replaying)
		{
			if (_replayCompletesRound && _resolvedRoundActivations.Count > 0)
			{
				_completedTurnOrders.Add(_resolvedRoundActivations.ToArray());
				_resolvedRoundActivations.Clear();
			}
			_currentTurnActivations = [];
			RebuildTurnFlowTimeline();
		}

		RefreshPresentation();
		_battleTutorial?.NotifyBattlePhaseChanged(phase);
	}

	private void BindActivePlayer()
	{
		var activeAgent = _battle.PlayerAgent;
		if (ReferenceEquals(_agent, activeAgent))
			return;

		var uePrev = _agent as UserExecutionAgent;
		uePrev?.PlanningChanged -= RefreshPresentation;
		_agent = activeAgent as UserExecutionAgent;
		_translator.Bind(_battle.ActivePlayerId, _agent as IActionSink);
		var ueNext = _agent as UserExecutionAgent;
		ueNext?.PlanningChanged += RefreshPresentation;
		_frames.Interaction.ResetAfterTurn();
	}

	private void OnTurnResolved(TurnReplay replay, int completedTurn)
	{
		foreach (var spawn in replay.History
			.OfType<Record<SpawnFacts>>()
			.Select(entry => entry.Value))
		{
			_unitTypes[spawn.SpawnedState.Id] = spawn.EntityType;
		}

		_resolvedRoundActivations.AddRange(replay.ActivationOrder);
		_replayCompletesRound = DidRoundComplete(completedTurn, _battle.TurnNumber);
		_currentTurnActivations = replay.ActivationOrder;
		RebuildTurnFlowTimeline();
		_frames.Interaction.ResetAfterTurn();
		_frames.AppendTurn(_battle, completedTurn, replay.History);
		if (_replayCompletesRound)
			_battleTutorial?.NotifyBattleTurnResolved(completedTurn);
		RefreshPresentation();
	}

	internal static bool DidRoundComplete(int resolvedTurn, int currentTurn) =>
		currentTurn > resolvedTurn;

	private void OnAbilityModeRequested(AbilityHudCatalog.Spec spec)
	{
		if (!AcceptsCommands)
			return;

		_frames.Interaction.SetMode(spec.Mode, spec);
		RefreshPresentation();
		if (AbilityActivation.ImmediateChoice(_currentFrame.AbilityChoices) is { } immediate)
			_translator.OnAbilityChoiceSelected(immediate);
	}

	private void OnAbilityConfirmed(AbilityActivationChoice choice)
	{
		var position = WorldMapping.ToWorld(choice.Position);
		switch (choice.Action)
		{
			case LightningCannonAction:
				PresentationSfx.PlayWorldOneShot(
					this,
					position,
					PresentationSfx.LightningCannonMountChargePath);
				break;
			case ScrapDroneSwarmAction:
				PresentationSfx.PlayScrapDroneMount(this, position);
				break;
		}
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
		_battleTutorial?.NotifyMoveSelectionStarted();
		RefreshPresentation();
	}

	private void OnMoveReopenRequested(Coord clickedCell)
	{
		if (_agent == null) return;
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
		var undone = (_agent as UserExecutionAgent)?.Undo() ?? false;
		if (!undone)
			RefreshPresentation();
		return undone;
	}

	private void RefreshPresentation()
	{
		if (_battle.Phase == EBattlePhase.PlayerTurn && !_replayPlayer.IsPlaying)
		{
			_currentTurnActivations = CurrentTurnActivations();
			RebuildTurnFlowTimeline();
			_battleHud.SetTurnFlowIndex(
				TurnFlowTimeline.StartOfCurrentSegment(_turnFlowTimeline));
		}

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
			movePoseSelectionActive: frame.IsMovePoseSelectionActive,
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
		if (_agent == null) return;

		var previewUnits = _frames.BuildFrame(_battle, _agent, acceptsCommands: false).PreviewUnits;
		if (!previewUnits.TryGetValue(unitId, out var unit) || !unit.IsAlive)
			return;

		_frames.Interaction.FocusUnit(unitId);
		RefreshPresentation();
	}

	private void CycleEnemyFocus()
	{
		if (!_battle.AcceptsPlayerInput)
			return;

		var playerPosition = _currentFrame.PreviewUnits[_battle.PlayerId].Position;
		var enemies = _currentFrame.PreviewUnits
			.Where(pair =>
				pair.Value.IsAlive
				&& _currentFrame.PreviewUnitTeams.TryGetValue(pair.Key, out var team)
				&& team == ETeam.Enemy)
			.OrderBy(pair =>
			{
				var offset = pair.Value.Position - playerPosition;
				return offset.X * offset.X + offset.Y * offset.Y + offset.Z * offset.Z;
			})
			.ThenBy(pair => pair.Key, StringComparer.Ordinal)
			.Select(pair => pair.Key)
			.ToArray();
		if (enemies.Length == 0)
			return;

		var currentEnemyId = _frames.Interaction.RangeEnemyId
			?? _currentFrame.RangeEnemy?.EnemyId;
		var current = currentEnemyId is { } enemyId
			? Array.IndexOf(enemies, enemyId)
			: -1;
		_frames.Interaction.SetRangeEnemy(enemies[(current + 1) % enemies.Length]);
		RefreshPresentation();
	}

	private void FocusUnitFromTopBar(string unitId)
	{
		if (_battle.Phase == EBattlePhase.Replaying)
		{
			if (!UnitRegistry.For(_battle.Engine.World).TryGet(unitId, out var liveUnit)
				|| !liveUnit.State.IsAlive)
				return;

			_frames.Interaction.FocusUnit(unitId);
			RefreshPresentation();
			return;
		}

		FocusUnit(unitId);
	}

	private void RebuildTurnFlowTimeline()
	{
		_turnFlowTimeline = TurnFlowTimeline.Build(
			_completedTurnOrders,
			_currentTurnActivations);
		_battleHud.SetTurnFlowTimeline(_turnFlowTimeline);
	}

	private void OnTurnFlowIndexChanged(int indexInTurn)
	{
		var globalIndex = TurnFlowTimeline.StartOfCurrentSegment(_turnFlowTimeline) + indexInTurn;
		_battleHud.SetTurnFlowIndex(globalIndex);
	}

	private IReadOnlyList<string> CurrentTurnActivations()
	{
		var registry = UnitRegistry.For(_battle.Engine.World);
		foreach (var unit in registry.All)
			_unitTypes[unit.State.Id] = unit.State.Type;

		return registry.ActivationOrder
			.Where(id => registry.TryGet(id, out var unit) && unit.State.IsAlive)
			.ToList();
	}

	private Texture2D? PortraitForUnit(string unitId)
	{
		if (_unitTypes.TryGetValue(unitId, out var knownType))
			return UnitPortraitCatalog.For(knownType);

		var registry = UnitRegistry.For(_battle.Engine.World);
		if (!registry.TryGet(unitId, out var unit))
			return null;

		_unitTypes[unitId] = unit.State.Type;
		return UnitPortraitCatalog.For(unit.State.Type);
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
		ApplyEnemyRangeTether(frame);
		_gridView.ApplyFrame(frame);
		_moveGhost.Apply(
			frame.MoveGhostState,
			frame.MoveCheckpoints,
			frame.FocusState,
			frame.ReachableMoveHeadings,
			ColorForFrameActor(frame, frame.FocusId),
			selected: frame.SelectedMove is not null);
		_targetOpportunityOverlay.Apply(frame.PreviewUnits, frame.PoseHitOpportunities);
		_abilitySourcePicker.Apply(
			frame.AbilityChoices.Count > 1 ? [] : frame.AbilityChoices,
			frame.AbilityHoveredIndex);
		_areaActionPreview.ApplyFrame(frame);
		_voidBombPreview.ApplyFrame(frame);
		_battleHud.Apply(frame, _battle.Phase, allowEndTurn: CanEndTurn);
		ApplyMountSelector(frame);
		ReportIssueSystemContext.SetBattleSnapshot(
			$"Battle turn: {frame.TurnNumber}, sim tick: {frame.SimulationTick}, mode: {frame.Mode}");
	}

	private void ApplyMountSelector(PresentationFrame frame)
	{
		if (frame.AbilityChoices.Count <= 1)
		{
			_battleHud.HideMountSelector();
			return;
		}

		if (frame.AbilityChoices.Any(choice => choice.MountedOn is null))
			throw new InvalidOperationException(
				"Multiple ability choices require mounted spatial orientations.");

		var legalFaces = frame.AbilityChoices
			.Select(choice => choice.MountedOn!.Value)
			.ToHashSet();
		var bodyFrame = frame.FocusState.ToBodyFrame();
		var faceDirections = Enum.GetValues<ESpatialOrientation>()
			.ToDictionary(face => face, bodyFrame.Step);
		var handles = MovementSelection.ProjectHeadingHandles(
			_camera,
			frame.FocusState.Position,
			faceDirections.Values.ToArray());
		if (handles.Count != faceDirections.Count)
		{
			_battleHud.HideMountSelector();
			return;
		}

		var handlesByDirection = handles.ToDictionary(handle => handle.Heading);
		var options = Enum.GetValues<ESpatialOrientation>()
			.Select(face =>
			{
				var handle = handlesByDirection[faceDirections[face]];
				return new MountFaceSelectorOption(
					face,
					HeadingDirectionColors.ForFace(face),
					legalFaces.Contains(face),
					handle.Position,
					handle.Rotation,
					handle.Kind);
			});
		var unitScreenPosition = _camera.UnprojectPosition(
			WorldMapping.ToWorld(frame.FocusState.Position));
		_battleHud.ShowMountSelector(unitScreenPosition, options);
	}

	private void ApplyEnemyRangeTether(PresentationFrame frame)
	{
		if (frame.RangeEnemy is not { } closest
			|| !_battleView.UnitViews.TryGetValue(_battle.PlayerId, out var player)
			|| !_battleView.UnitViews.TryGetValue(closest.EnemyId, out var enemy))
		{
			_enemyRangeTether.Apply(null, null, 0);
			return;
		}

		_enemyRangeTether.Apply(
			player,
			enemy,
			closest.SeparationMeters,
			frame.PreviewUnits[_battle.PlayerId],
			frame.PreviewUnits[closest.EnemyId]);
	}

	private void UpdateAsteroidOcclusionTargets(PresentationFrame frame)
	{
		var targets = new List<Vector3> { GetPlayerRenderedPosition() };

		if (frame.MoveGhostState is { } moveGhost)
			targets.Add(WorldMapping.ToWorld(moveGhost.Position));

		if (frame.HoveredAbilityChoice is { } ability)
			targets.Add(WorldMapping.ToWorld(ability.Position));

		foreach (var targetId in frame.ThreatenedUnitIds)
		{
			if (_battleView.UnitViews.TryGetValue(targetId, out var view))
				targets.Add(view.GlobalPosition);
			else if (frame.PreviewUnits.TryGetValue(targetId, out var state))
				targets.Add(WorldMapping.ToWorld(state.Position));
		}

		_asteroidView.SetOcclusionTargets(targets.Distinct().ToArray());
	}

	private void OnTutorialFlowCompleted(TutorialFlow _) => RefreshPresentation();

	private Vector3 GetPlayerRenderedPosition()
	{
		var playerId = _battle.PlayerId;
		if (_battleView.UnitViews.TryGetValue(playerId, out var view))
			return view.GlobalPosition;

		if (_agent == null) return Vector3.Zero;
		return WorldMapping.ToWorld(_agent.Sim.StateOf<ActorState>(playerId).Position);
	}

	private void ApplyUnitStates(PresentationFrame frame)
	{
		var states = frame.PreviewUnits.ToDictionary(
			entry => entry.Key,
			entry => entry.Value.ToState());
		Color FrameColor(string actorId) => ColorForFrameActor(frame, actorId);
		if (UsesPlanningPreviewForFrameUnitStates(_battle.Phase))
			_battleView.ApplyPlanningPreview(states, FrameColor);
		else
			_battleView.ApplyUnitStates(states, FrameColor);
		_battleView.ApplyHitMarks(frame.ThreatenedUnitIds);
	}

	private Color ColorForFrameActor(PresentationFrame frame, string actorId) =>
		frame.PreviewUnitTeams.TryGetValue(actorId, out var team)
			? ColorFor(team)
			: ColorForActor(actorId);

	private void SyncAuthoritativeUnitViewsFromWorld()
	{
		var states = UnitRegistry.For(_battle.Engine.World).All
			.Where(unit => unit.State.IsAlive)
			.ToDictionary(unit => unit.State.Id, unit => unit.State);
		_battleView.ApplyUnitStates(states, ColorForActor);
	}

	private void SyncAuthoritativeHazardViews()
	{
		_goopHazardView.Apply(
			_battle.Engine.World.NonUnits.Values.OfType<GoopHazard>());
	}

	private void ApplyReplayState(ActorState state)
	{
		if (_currentFrame.FocusState.Id == state.Id)
			_battleHud.CommandHud.ApplyDefenses(
				state.HullPoints,
				state.Loadout.MaxHullPoints,
				state.ShieldPoints,
				state.Loadout.MaxShieldPoints);
	}

	internal static bool ShouldApplyFrameUnitStates(EBattlePhase phase) =>
		phase != EBattlePhase.Replaying;

	internal static bool UsesPlanningPreviewForFrameUnitStates(EBattlePhase phase) =>
		phase is EBattlePhase.PlayerTurn or EBattlePhase.Resolving;

	internal static bool ShouldAllowEndTurn(bool acceptsCommands, bool tutorialBlocksEndTurn) =>
		acceptsCommands && !tutorialBlocksEndTurn;

	private void OnOutcomeOverlayAction()
	{
		if (_strategicBattle && _currentFrame.Outcome == EBattleResult.Lose)
			GoToMainMenu();
		else if (_strategicBattle)
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
			var result = Session.Instance.TrySaveBeforeLeavingRun(_battle.Engine.World);
			if (result != GrimSpace.Run.Persistence.SaveStorageResult.Success)
				GD.PrintErr($"Failed to save game before returning to main menu: {result}");
			GetTree().ChangeSceneToFile("res://scenes/main.tscn");
		}

	public async Task RunAiVsAiAsync(int maxTurns = 8, Action<int, string>? onTurnLog = null)
	{
		var sw = Stopwatch.StartNew();
		var phaseTcs = new TaskCompletionSource<EBattlePhase>();
		_battle.PhaseChanged += phase =>
		{
			phaseTcs.TrySetResult(phase);
			phaseTcs = new TaskCompletionSource<EBattlePhase>();
		};

		for (var turn = 0; turn < maxTurns && !_battle.IsBattleOver; turn++)
		{
			if (_battle.Phase == EBattlePhase.PlayerTurn)
			{
				var activeUnit = UnitRegistry.For(_battle.Engine.World).UnitOf(_battle.ActivePlayerId);
				if (activeUnit.ExecutionAgent is AiExecutionAgent aiAgent)
				{
					await WaitForAgentAsync(aiAgent);
					aiAgent.Commit();
					_battle.EndTurn();
				}
			}

			while (_battle.Phase == EBattlePhase.Resolving || _battle.Phase == EBattlePhase.Replaying)
			{
				await Task.WhenAny(phaseTcs.Task, Task.Delay(100));
				
				RefreshPresentation();
				
				if (sw.ElapsedMilliseconds > 300000)
					throw new TimeoutException("Battle resolution timeout");
			}

			RefreshPresentation();

			onTurnLog?.Invoke(turn, $"Turn {turn}: Phase={_battle.Phase}, Result={_battle.Engine.World.battleResult}");
		}

		sw.Stop();
	}

	private static async Task WaitForAgentAsync(AiExecutionAgent agent, int timeoutMs = 5000)
	{
		var sw = Stopwatch.StartNew();
		while (agent.IsPlanning && sw.ElapsedMilliseconds < timeoutMs)
		{
			await Task.Delay(16);
		}
		if (agent.IsPlanning)
			throw new TimeoutException("AI agent planning timeout");
	}

	private static Color ColorFor(ETeam team) =>
		team switch
		{
			ETeam.Player => new Color(0.25f, 0.85f, 0.35f),
			ETeam.Enemy => new Color(0.9f, 0.25f, 0.2f),
			_ => Colors.White,
		};

	/// <summary>
	/// Sets up an overview camera that frames all units in the battle.
	/// Useful for teaser/demo scenarios where player-following camera isn't desired.
	/// </summary>
	public void SetupOverviewCamera()
	{
		var unitViews = _battleView.UnitViews;
		GD.Print($"[Teaser] SetupOverviewCamera: unitViews.Count = {unitViews.Count}");
		if (unitViews.Count == 0)
		{
			GD.Print("[Teaser] No unit views found");
			return;
		}

		var positions = new List<Vector3>();
		foreach (var kvp in unitViews)
		{
			var view = kvp.Value;
			GD.Print($"[Teaser] Unit {kvp.Key}: Visible={view.Visible}, GlobalPosition={view.GlobalPosition}, VisualState={view.VisualState}");
			if (view.Visible)
				positions.Add(view.GlobalPosition);
		}

		if (positions.Count == 0)
		{
			GD.Print("[Teaser] No visible unit positions found");
			return;
		}

		var center = Vector3.Zero;
		foreach (var p in positions)
			center += p;
		center /= positions.Count;

		float maxDist = 0f;
		foreach (var p in positions)
		{
			var dist = (p - center).Length();
			if (dist > maxDist) maxDist = dist;
		}

		// Add padding and set camera distance
		float padding = 1.5f;
		float distance = (maxDist + 5f) * 1.5f; // 5f = unit visual radius estimate

		// Clamp to camera limits
		distance = Mathf.Clamp(distance, 8f, 280f);

		// Use a top-down-ish overview angle
		float yaw = 0f; // Looking from positive X
		float pitch = -0.4f; // ~23 degrees down

		// Pull back further to see all units, but not too far
		distance *= 1.5f;

		// Clamp again after scaling
		distance = Mathf.Clamp(distance, 8f, 280f);

		_camera.SetFocus(center, distance, yaw, pitch);

		GD.Print($"[Teaser] Overview camera set - Center={center}, Distance={distance}, Yaw=0, Pitch=-0.4, UnitCount={positions.Count}");
	}

	/// <summary>
	/// Disables camera tracking during replay for teaser/demo scenarios.
	/// Keeps the camera static at the overview position.
	/// </summary>
	public void EnableStaticCameraMode()
	{
		_cameraDirector.EnableStaticMode();
		GD.Print("[Teaser] Static camera mode enabled - camera will not move during replay");
	}

	public override void _ExitTree()
	{
		Session.Instance.ClearAutosaveContext();
		_camera.ManualInputStarted -= _cameraDirector.OnManualInputStarted;
		_camera.ManualInputStarted -= _translator.OnCameraManualInputStarted;
		_replayPlayer.TurnFlowIndexChanged -= OnTurnFlowIndexChanged;
		(_agent as UserExecutionAgent)?.PlanningChanged -= RefreshPresentation;
		_battle.PhaseChanged -= OnPhaseChanged;
		_battle.TurnResolved -= OnTurnResolved;
		if (Session.Instance.Run.Tutorials is { } tutorials)
			tutorials.FlowCompleted -= OnTutorialFlowCompleted;
		_battleTutorial?.Dispose();
		_battleTutorial = null;
		Session.Instance.DevMenu.ClearBattleActions();
		_cellVolumeMeshes?.Dispose();
		_battle?.Dispose();
		ReportIssueSystemContext.SetBattleSnapshot(null);
		base._ExitTree();
	}
}
