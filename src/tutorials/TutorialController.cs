using Godot;
using GrimSpace.Application;
using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Education;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Objectives;

namespace GrimSpace.Tutorials;

public sealed class TutorialController : IDisposable
{
	private const int TutorialTurn1 = 1;
	private const int TutorialTurn2 = 2;

	private readonly StarSystemOrchestrator? _orchestrator;
	private readonly BattleOrchestrator? _battle;
	private readonly UserExecutionAgent? _battleAgent;
	private readonly TutorialProgress _progress;
	private readonly TutorialRunner _runner;
	private readonly TutorialGhostPresenter? _ghostPresenter;
	private readonly HashSet<string> _reportedStartFailures = new(StringComparer.Ordinal);
	private readonly TutorialContractScheduler? _contractScheduler;
	private IDisposable? _narrativeSubscription;
	private IDisposable? _activeActionSubscription;
	private BattleTutorialObjective? _turn1Objective;
	private BattleTutorialObjective? _turn2Objective;
	private bool _turn2AwaitingPlayerTurn;

	public event Action<TutorialFlow>? Completed;
	public event Action<TutorialFlow, TutorialStep>? StepStarted;
	public TutorialController(
		StarSystemOrchestrator orchestrator,
		TutorialProgress progress,
		ITutorialDialog dialog,
		WorldLinkNavigator worldLinks)
		: this(progress, dialog, worldLinks, battle: null, battleAgent: null)
	{
		_orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
		_contractScheduler = new TutorialContractScheduler(_orchestrator);
		_contractScheduler.Start();
		_narrativeSubscription = _orchestrator.Subscribe<CompleteNarrativeAction>(_ => Sync());
	}

	private TutorialController(
		TutorialProgress progress,
		ITutorialDialog dialog,
		WorldLinkNavigator worldLinks,
		BattleOrchestrator? battle,
		UserExecutionAgent? battleAgent,
		PosedUnitGhostView? battleGhost = null)
	{
		_progress = progress ?? throw new ArgumentNullException(nameof(progress));
		_battle = battle;
		_battleAgent = battleAgent;
		_ghostPresenter = battleGhost is null ? null : new TutorialGhostPresenter(battleGhost);
		_runner = new TutorialRunner(progress, dialog, worldLinks);
		_runner.Started += OnStarted;
		_runner.StepStarted += OnStepStarted;
		_runner.AssistanceRequested += OnAssistanceRequested;
		_runner.Completed += OnCompleted;
		if (_battleAgent is not null)
			_battleAgent.PlanningChanged += OnBattlePlanningChanged;
	}

	public static TutorialController CreateForBattle(
		BattleOrchestrator battle,
		TutorialProgress progress,
		ITutorialDialog dialog,
		WorldLinkNavigator worldLinks,
		PosedUnitGhostView ghost)
	{
		ArgumentNullException.ThrowIfNull(battle);
		ArgumentNullException.ThrowIfNull(ghost);
		var controller = new TutorialController(
			progress,
			dialog,
			worldLinks,
			battle,
			battle.PlayerAgent,
			ghost);
		controller.TryStartBattleTutorial();
		return controller;
	}

	public bool IsActive => _runner.ActiveFlow is not null;

	public bool AllowsEndTurn =>
		_runner.ActiveStep?.TargetId is FirstBattleTutorial.Turn1EndTargetId
			or FirstBattleTutorial.Turn2EndTargetId;

	public void Sync()
	{
		if (_orchestrator is null)
			throw new InvalidOperationException("Only star-system tutorial controllers can be synchronized.");
		if (IsActive)
			return;

		var flow = NextFlow();
		if (flow is null)
			return;

		Start(flow);
	}

	private void TryStartBattleTutorial()
	{
		var battle = _battle
			?? throw new InvalidOperationException("Battle tutorial requires a battle orchestrator.");
		var player = battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId);
		var initialBasis = GridBasis.From(player.Fore, player.Dorsal, player.Starboard);

		var turn1Result = BattleTutorialObjectives.ResolveTurn1(battle, initialBasis);
		if (turn1Result is BattleTutorialObjectiveResult.Unreachable unreachable)
		{
			GD.PushWarning($"First battle tutorial setup failed: {unreachable.Reason}");
			return;
		}

		_turn1Objective = ((BattleTutorialObjectiveResult.Resolved)turn1Result).Objective;
		Start(FirstBattleTutorial.Create());
	}

	private void Start(TutorialFlow flow)
	{
		var result = _runner.Start(flow);
		if (result is TutorialStartResult.Started or TutorialStartResult.AlreadyCompleted)
			return;

		if (_reportedStartFailures.Add(flow.Id))
		{
			GD.PushWarning(
				$"Tutorial '{flow.Id}' could not start: {result.GetType().Name}.");
		}
	}

	private TutorialFlow? NextFlow()
	{
		var orchestrator = _orchestrator
			?? throw new InvalidOperationException("Battle tutorial controllers do not select star-system flows.");
		if (!_progress.IsCompleted(FirstContractTutorial.Id)
			&& orchestrator.Map.StoryObjectives.Active.Any(
				objective => objective.Id == StoryObjective.FirstContract.Id))
			return FirstContractTutorial.Create(orchestrator.Map);

		return null;
	}

	private void OnStarted(TutorialFlow flow)
	{
		if (_orchestrator is null)
			return;

		_activeActionSubscription?.Dispose();
		_activeActionSubscription = flow.Id switch
		{
			FirstContractTutorial.Id => _orchestrator.Subscribe<MoveAction>(OnMove),
			_ => null,
		};
	}

	private void OnCompleted(TutorialFlow flow)
	{
		_activeActionSubscription?.Dispose();
		_activeActionSubscription = null;
		_turn2AwaitingPlayerTurn = false;
		_ghostPresenter?.SetSpec(null);
		if (flow.Id == FirstBattleTutorial.Id)
			GameSettings.SaveShowTutorials(false);
		Completed?.Invoke(flow);
	}

	private void OnStepStarted(TutorialFlow flow, TutorialStep step)
	{
		StepStarted?.Invoke(flow, step);
		if (flow.Id == FirstBattleTutorial.Id)
		{
			UpdateGhostForActiveStep();
			AdvanceBattleStepIfReady();
		}
	}

	private void OnBattlePlanningChanged()
	{
		if (_battleAgent?.Sim.Actions.Count == 0
			&& _runner.ActiveStep?.TargetId is FirstBattleTutorial.Turn1MoveTargetId
				or FirstBattleTutorial.Turn2MoveTargetId)
		{
			_ghostPresenter?.Restore();
		}

		AdvanceBattleStepIfReady();
	}

	private void AdvanceBattleStepIfReady()
	{
		if (_battleAgent is null
			|| _battle is null
			|| _runner.ActiveFlow is not { Id: FirstBattleTutorial.Id })
			return;

		switch (_runner.ActiveStep?.TargetId)
		{
			case FirstBattleTutorial.Turn1MoveTargetId:
				AdvanceBattleMoveIfReady(_turn1Objective, TutorialCopy.MoveToMarkedGhostAssistance);
				break;
			case FirstBattleTutorial.Turn2MoveTargetId:
				AdvanceBattleMoveIfReady(_turn2Objective, TutorialCopy.MatchGhostPoseAssistance);
				break;
			case FirstBattleTutorial.Turn2TorpedoTargetId:
				AdvanceBattleTorpedoIfReady();
				break;
		}
	}

	private void AdvanceBattleMoveIfReady(BattleTutorialObjective? objective, string mismatchMessage)
	{
		if (objective is null)
			return;

		var battleAgent = _battleAgent
			?? throw new InvalidOperationException("Battle movement requires a battle agent.");
		if (battleAgent.Sim.Actions.Count == 0)
		{
			_runner.ClearAssistance();
			return;
		}

		if (QueuedMovementMatchesObjective(battleAgent, objective))
		{
			_runner.ClearAssistance();
			_runner.AdvanceActive();
			return;
		}

		_runner.ShowAssistance(new TutorialAssistanceContent(
			mismatchMessage,
			TutorialCopy.UndoAndRetryAssistance));
	}

	private void AdvanceBattleTorpedoIfReady()
	{
		var battleAgent = _battleAgent
			?? throw new InvalidOperationException("Battle torpedo step requires a battle agent.");
		if (_turn2Objective is null || !QueuedMovementMatchesObjective(battleAgent, _turn2Objective))
		{
			_runner.ShowAssistance(new TutorialAssistanceContent(
				TutorialCopy.MatchGhostPoseAssistance,
				TutorialCopy.UndoAndRetryAssistance));
			return;
		}

		if (!battleAgent.Sim.Actions.Any(action =>
				action is TorpedoAction { MountedOn: ESpatialOrientation.Ventral }))
		{
			if (battleAgent.Sim.Actions.Any(action => action is TorpedoAction))
			{
				_runner.ShowAssistance(new TutorialAssistanceContent(
					TutorialCopy.QueueVentralTorpedoAssistance,
					TutorialCopy.UndoAndRetryAssistance));
			}
			else
			{
				_runner.ClearAssistance();
			}

			return;
		}

		_runner.ClearAssistance();
		_runner.AdvanceActive();
	}

	private bool QueuedMovementMatchesObjective(
		UserExecutionAgent battleAgent,
		BattleTutorialObjective objective)
	{
		var battle = _battle
			?? throw new InvalidOperationException("Battle objective validation requires a battle orchestrator.");
		var movementActions = MovementPrefix(battleAgent.Sim.Actions);
		if (movementActions.Count == 0)
			return false;

		var checkpoints = MovePathIndex.ProjectCheckpoints(
			battleAgent.Sim.ForkFromAnchor(),
			battle.PlayerId,
			movementActions,
			includeStart: false);
		if (checkpoints.Count == 0)
			return false;

		var end = checkpoints[^1];
		return end.Position == objective.Destination && end.Basis == objective.RequiredBasis;
	}

	private void OnAssistanceRequested()
	{
		if (_battleAgent is null
			|| _runner.ActiveFlow is not { Id: FirstBattleTutorial.Id })
			return;

		switch (_runner.ActiveStep?.TargetId)
		{
			case FirstBattleTutorial.Turn1MoveTargetId:
			case FirstBattleTutorial.Turn2MoveTargetId:
			case FirstBattleTutorial.Turn2TorpedoTargetId:
				if (!_battleAgent.Undo())
					GD.PushWarning("Tutorial could not undo the invalid battle plan.");
				break;
		}
	}

	private static List<IAction> MovementPrefix(IReadOnlyList<IAction> actions)
	{
		var movement = new List<IAction>();
		foreach (var action in actions)
		{
			if (action is MoveStepAction or HeadingTurnAction or RollAction)
			{
				movement.Add(action);
				continue;
			}

			break;
		}

		return movement;
	}

	public void NotifyMoveSelectionStarted() => _ghostPresenter?.Suppress();

	public void NotifyMoveSelectionCanceled() => _ghostPresenter?.Restore();

	public void NotifyBattlePhaseChanged(EBattlePhase phase)
	{
		if (_runner.ActiveFlow is not { Id: FirstBattleTutorial.Id })
			return;

		switch (phase)
		{
			case EBattlePhase.Resolving
				when _runner.ActiveStep?.TargetId == FirstBattleTutorial.Turn1EndTargetId:
				_runner.AdvanceActiveSilently();
				break;
			case EBattlePhase.PlayerTurn:
				TryPresentTurn2();
				break;
		}
	}

	public void NotifyBattleTurnResolved(int completedTurn)
	{
		if (_runner.ActiveFlow is not { Id: FirstBattleTutorial.Id })
			return;

		switch (completedTurn)
		{
			case TutorialTurn1
				when _runner.ActiveStep?.TargetId == FirstBattleTutorial.Turn2MoveTargetId:
				_turn2AwaitingPlayerTurn = PrepareTurn2Objective();
				break;
			case TutorialTurn2
				when _runner.ActiveStep?.TargetId == FirstBattleTutorial.Turn2EndTargetId:
				_runner.AdvanceActive();
				break;
		}
	}

	private void TryPresentTurn2()
	{
		if (!_turn2AwaitingPlayerTurn
			|| _runner.ActiveStep?.TargetId != FirstBattleTutorial.Turn2MoveTargetId
			|| _turn2Objective is not { } objective)
			return;

		var battle = _battle
			?? throw new InvalidOperationException("Battle tutorial requires a battle orchestrator.");
		if (!BattleTutorialObjectives.IsReachable(battle, objective))
		{
			AbortBattleTutorial("First battle tutorial turn 2 setup failed: Turn 2 objective is unreachable.");
			return;
		}

		_turn2AwaitingPlayerTurn = false;
		_runner.PresentActiveStep();
	}

	private bool PrepareTurn2Objective()
	{
		var battle = _battle
			?? throw new InvalidOperationException("Battle tutorial requires a battle orchestrator.");
		var turn2Result = BattleTutorialObjectives.ResolveTurn2(battle);
		if (turn2Result is BattleTutorialObjectiveResult.Unreachable unreachable)
		{
			AbortBattleTutorial($"First battle tutorial turn 2 setup failed: {unreachable.Reason}");
			return false;
		}

		_turn2Objective = ((BattleTutorialObjectiveResult.Resolved)turn2Result).Objective;
		UpdateGhostForActiveStep();
		return true;
	}

	private void AbortBattleTutorial(string reason)
	{
		GD.PushWarning(reason);
		_turn2AwaitingPlayerTurn = false;
		_ghostPresenter?.SetSpec(null);
		_runner.CancelActive();
	}

	private void UpdateGhostForActiveStep()
	{
		if (_battle is null)
		{
			_ghostPresenter?.SetSpec(null);
			return;
		}

		var objective = _runner.ActiveStep?.TargetId switch
		{
			FirstBattleTutorial.Turn1MoveTargetId => _turn1Objective,
			FirstBattleTutorial.Turn2MoveTargetId => _turn2Objective,
			_ => null,
		};
		if (objective is null)
		{
			_ghostPresenter?.SetSpec(null);
			return;
		}

		var player = _battle.Engine.World.StateOf(_battle.PlayerId);
		_ghostPresenter?.SetSpec(new PosedUnitGhostSpec(
			player.Type,
			objective.Destination,
			objective.RequiredBasis.Forward,
			objective.RequiredBasis.Up,
			Colors.Cyan));
	}

	private void OnMove(MoveAction move)
	{
		var orchestrator = _orchestrator
			?? throw new InvalidOperationException("Battle tutorials do not observe star-system movement.");
		if (_runner.ActiveFlow is not { Id: FirstContractTutorial.Id }
			|| _runner.ActiveStep?.TargetId is not { } targetId
			|| move.ActorId != orchestrator.PlayerId
			|| move.UnitId != orchestrator.PlayerId
			|| !orchestrator.Map.DocksByPoiId.TryGetValue(targetId, out var dock)
			|| move.Destination != dock.Position)
			return;

		_runner.AdvanceActive();
	}

	public void Dispose()
	{
		_contractScheduler?.Dispose();
		_narrativeSubscription?.Dispose();
		if (_battleAgent is not null)
			_battleAgent.PlanningChanged -= OnBattlePlanningChanged;
		_runner.Started -= OnStarted;
		_runner.StepStarted -= OnStepStarted;
		_runner.AssistanceRequested -= OnAssistanceRequested;
		_runner.Completed -= OnCompleted;
		_activeActionSubscription?.Dispose();
		_activeActionSubscription = null;
		_ghostPresenter?.Dispose();
		_runner.Dispose();
	}
}
