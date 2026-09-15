using Godot;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Player;
using GrimSpace.Education;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Objectives;

namespace GrimSpace.Tutorials;

public sealed class TutorialController : IDisposable
{
	private readonly StarSystemOrchestrator? _orchestrator;
	private readonly UserExecutionAgent? _battleAgent;
	private readonly TutorialProgress _progress;
	private readonly TutorialRunner _runner;
	private readonly HashSet<string> _reportedStartFailures = new(StringComparer.Ordinal);
	private IDisposable? _narrativeSubscription;
	private IDisposable? _activeActionSubscription;

	public event Action<TutorialFlow>? Completed;
	public event Action<TutorialFlow, TutorialStep>? StepStarted;

	public TutorialController(
		StarSystemOrchestrator orchestrator,
		TutorialProgress progress,
		ITutorialDialog dialog,
		IWorldFocus worldFocus,
		IWorldIndicator worldIndicator)
		: this(progress, dialog, worldFocus, worldIndicator, battleAgent: null)
	{
		_orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
		_narrativeSubscription = _orchestrator.Subscribe<CompleteNarrativeAction>(_ => Sync());
	}

	private TutorialController(
		TutorialProgress progress,
		ITutorialDialog dialog,
		IWorldFocus worldFocus,
		IWorldIndicator worldIndicator,
		UserExecutionAgent? battleAgent)
	{
		_progress = progress ?? throw new ArgumentNullException(nameof(progress));
		_battleAgent = battleAgent;
		_runner = new TutorialRunner(progress, dialog, worldFocus, worldIndicator);
		_runner.Started += OnStarted;
		_runner.StepStarted += OnStepStarted;
		_runner.AssistanceRequested += OnAssistanceRequested;
		_runner.Completed += OnCompleted;
		if (_battleAgent is not null)
			_battleAgent.PlanningChanged += OnBattlePlanningChanged;
	}

	public static TutorialController CreateForBattle(
		UserExecutionAgent battleAgent,
		TutorialProgress progress,
		ITutorialDialog dialog,
		IWorldFocus worldFocus,
		IWorldIndicator worldIndicator)
	{
		ArgumentNullException.ThrowIfNull(battleAgent);
		var controller = new TutorialController(
			progress,
			dialog,
			worldFocus,
			worldIndicator,
			battleAgent);
		controller.Start(FirstBattleTutorial.Create());
		return controller;
	}

	public bool IsActive => _runner.ActiveFlow is not null;

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
		{
			return FirstContractTutorial.Create(orchestrator.Map);
		}

		if (_progress.IsCompleted(FirstContractTutorial.Id)
			&& !_progress.IsCompleted(FirstPirateTutorial.Id))
		{
			return FirstPirateTutorial.CreateForActiveContract(orchestrator.Map);
		}

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
			FirstPirateTutorial.Id => _orchestrator.Subscribe<HuntUnitAction>(OnHunt),
			_ => null,
		};
	}

	private void OnCompleted(TutorialFlow flow)
	{
		_activeActionSubscription?.Dispose();
		_activeActionSubscription = null;
		if (flow.Id == FirstBattleTutorial.Id)
			ClearBattlePlan();
		Completed?.Invoke(flow);
	}

	private void OnStepStarted(TutorialFlow flow, TutorialStep step)
	{
		StepStarted?.Invoke(flow, step);
		if (flow.Id == FirstBattleTutorial.Id)
			AdvanceBattleStepIfReady();
	}

	private void ClearBattlePlan()
	{
		var battleAgent = _battleAgent
			?? throw new InvalidOperationException("Battle tutorial completion requires a battle agent.");
		while (battleAgent.Sim.Actions.Count > 0)
		{
			if (battleAgent.Undo())
				continue;

			GD.PushWarning("Tutorial could not clear the remaining battle plan.");
			break;
		}
	}

	private void OnBattlePlanningChanged() => AdvanceBattleStepIfReady();

	private void AdvanceBattleStepIfReady()
	{
		if (_battleAgent is null
			|| _runner.ActiveFlow is not { Id: FirstBattleTutorial.Id })
			return;

		switch (_runner.ActiveStep?.TargetId)
		{
			case FirstBattleTutorial.PlanMovementTargetId:
				AdvanceBattleMovementIfReady();
				break;
			case FirstBattleTutorial.QueueFlakTargetId:
				if (_battleAgent.Sim.Actions.Any(action => action is FlakAction))
					_runner.AdvanceActive();
				break;
			case FirstBattleTutorial.UndoFlakTargetId:
				if (_battleAgent.Sim.Actions.All(action => action is not FlakAction))
					_runner.AdvanceActive();
				break;
		}
	}

	private void AdvanceBattleMovementIfReady()
	{
		var battleAgent = _battleAgent
			?? throw new InvalidOperationException("Battle movement requires a battle agent.");
		var actions = battleAgent.Sim.Actions;
		var hasMove = actions.Any(action => action is MoveStepAction);
		var hasRoll = actions.Any(action => action is RollAction);
		if (hasMove && hasRoll)
		{
			_runner.AdvanceActive();
			return;
		}

		if (actions.Count == 0)
		{
			_runner.ClearAssistance();
			return;
		}

		var message = hasMove
			? "This maneuver has no roll. Undo it, then pick a destination and scroll before releasing the mouse."
			: "This plan has no movement. Undo it, then pick a destination inside the movement bubble.";
		_runner.ShowAssistance(new TutorialAssistanceContent(message, "Undo and try again"));
	}

	private void OnAssistanceRequested()
	{
		if (_battleAgent is null
			|| _runner.ActiveFlow is not { Id: FirstBattleTutorial.Id }
			|| _runner.ActiveStep?.TargetId != FirstBattleTutorial.PlanMovementTargetId)
		{
			return;
		}

		if (!_battleAgent.Undo())
			GD.PushWarning("Tutorial could not undo the invalid battle plan.");
	}

	public void NotifyBattleUndoShortcut()
	{
		if (_battleAgent is null)
			throw new InvalidOperationException("Only battle tutorial controllers accept undo shortcuts.");
		if (_runner.ActiveFlow is { Id: FirstBattleTutorial.Id }
			&& _runner.ActiveStep?.TargetId == FirstBattleTutorial.UndoTargetId)
		{
			_runner.AdvanceActive();
		}
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

	private void OnHunt(HuntUnitAction hunt)
	{
		var orchestrator = _orchestrator
			?? throw new InvalidOperationException("Battle tutorials do not observe star-system hunts.");
		if (_runner.ActiveFlow is not { Id: FirstPirateTutorial.Id }
			|| _runner.ActiveStep?.TargetId is not { } targetId
			|| hunt.ActorId != orchestrator.PlayerId
			|| hunt.TargetUnitId != targetId)
			return;

		_runner.AdvanceActive();
	}

	public void Dispose()
	{
		_narrativeSubscription?.Dispose();
		if (_battleAgent is not null)
			_battleAgent.PlanningChanged -= OnBattlePlanningChanged;
		_runner.Started -= OnStarted;
		_runner.StepStarted -= OnStepStarted;
		_runner.AssistanceRequested -= OnAssistanceRequested;
		_runner.Completed -= OnCompleted;
		_activeActionSubscription?.Dispose();
		_activeActionSubscription = null;
		_runner.Dispose();
	}
}
