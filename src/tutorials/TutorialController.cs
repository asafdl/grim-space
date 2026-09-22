using GrimSpace.Core.Log;
using GrimSpace.Education;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Objectives;

namespace GrimSpace.Tutorials;

public sealed class TutorialController : IDisposable
{
	private readonly StarSystemOrchestrator _orchestrator;
	private readonly TutorialProgress _progress;
	private readonly TutorialState _state;
	private readonly ITutorialRunContext _runContext;
	private readonly HashSet<string> _reportedStartFailures = new(StringComparer.Ordinal);
	private IDisposable? _contractCompletionSubscription;
	private IDisposable? _narrativeSubscription;
	private TutorialFlow? _activeFlow;

	public TutorialController(
		StarSystemOrchestrator orchestrator,
		TutorialProgress progress,
		TutorialState state,
		ITutorialRunContext runContext)
	{
		_orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
		_progress = progress ?? throw new ArgumentNullException(nameof(progress));
		_state = state ?? throw new ArgumentNullException(nameof(state));
		_runContext = runContext ?? throw new ArgumentNullException(nameof(runContext));
	}

	public TutorialState State => _state;

	public TutorialProgress Progress => _progress;

	public event Action<TutorialFlow>? FlowStarted;

	public event Action<TutorialFlow, TutorialStep, bool>? StepPresented;

	public event Action<TutorialFlow>? FlowCompleted;

	public event Action? AssistanceRequested;

	public TutorialFlow? ActiveFlow => _activeFlow;

	public TutorialStep? ActiveStep =>
		_activeFlow is { } flow && _state.ActiveStepIndex >= 0
			? flow.Steps[_state.ActiveStepIndex]
			: null;

	public bool IsActive => _activeFlow is not null;

	public void InitializeBeatProgression()
	{
		if (_state.CurrentBeat != TutorialBeat.None)
			return;

		_state.BeatAContractId = TutorialBeatContracts.OfferBeatA(_orchestrator.Map);
		_state.CurrentBeat = TutorialBeat.FirstContract;
		GameLog.Log($"Tutorial initialized: beatAContractId='{_state.BeatAContractId}'.");
		RegisterContractObservation();
		ReconcileBeatTransitions();
	}

	public void AttachMapSubscriptions()
	{
		_narrativeSubscription ??= _orchestrator.Subscribe<CompleteNarrativeAction>(
			_ => SyncMapFlows(cancelBattleFlowWhenOffBattlefield: false));
		RegisterContractObservation();
	}

	public void DetachMapSubscriptions()
	{
		_narrativeSubscription?.Dispose();
		_narrativeSubscription = null;
	}

	public void ReconcileFromWorldState(bool cancelBattleFlowWhenOffBattlefield)
	{
		ReconcileBeatTransitions();
		ReconcileFlowProgress(cancelBattleFlowWhenOffBattlefield);
	}

	public void SyncMapFlows(bool cancelBattleFlowWhenOffBattlefield = false) =>
		ReconcileFromWorldState(cancelBattleFlowWhenOffBattlefield);

	public void PresentGraduationIfPending()
	{
		ReconcileFlowProgress(cancelBattleFlowWhenOffBattlefield: true);

		if (!_runContext.PendingTutorialGraduation)
			return;

		if (_progress.IsCompleted(TutorialGraduation.Id))
		{
			_runContext.PendingTutorialGraduation = false;
			return;
		}

		if (IsActive)
			return;

		_state.CurrentBeat = TutorialBeat.Graduation;
		TryStartFlow(TutorialGraduation.Create());
	}

	public TutorialStartResult TryStartFlow(TutorialFlow flow)
	{
		ArgumentNullException.ThrowIfNull(flow);
		ValidateFlow(flow);

		if (_progress.IsCompleted(flow.Id))
			return new TutorialStartResult.AlreadyCompleted();

		if (_activeFlow is { } active)
			return new TutorialStartResult.Busy(active.Id);

		_activeFlow = flow;
		_state.ActiveFlowId = flow.Id;
		_state.ActiveStepIndex = 0;
		FlowStarted?.Invoke(flow);
		RepresentActiveStep();
		return new TutorialStartResult.Started();
	}

	public void RepresentActiveStep(bool openDialog = true)
	{
		if (_activeFlow is null || ActiveStep is not { } step)
			return;

		StepPresented?.Invoke(_activeFlow, step, openDialog);
	}

	public TutorialAdvanceResult AdvanceActive() => AdvanceActive(clearDialogBeforeNext: false);

	public TutorialAdvanceResult AdvanceActiveSilently() =>
		AdvanceActive(clearDialogBeforeNext: true);

	private TutorialAdvanceResult AdvanceActive(bool clearDialogBeforeNext)
	{
		if (_activeFlow is not { } flow)
			return new TutorialAdvanceResult.NoActiveFlow();

		var nextStepIndex = _state.ActiveStepIndex + 1;
		if (nextStepIndex >= flow.Steps.Count)
		{
			CompleteActiveFlow();
			return new TutorialAdvanceResult.Completed();
		}

		_state.ActiveStepIndex = nextStepIndex;
		if (clearDialogBeforeNext)
		{
			StepPresented?.Invoke(flow, flow.Steps[nextStepIndex], false);
			return new TutorialAdvanceResult.Advanced();
		}

		RepresentActiveStep(openDialog: true);
		return new TutorialAdvanceResult.Advanced();
	}

	public event Action<TutorialAssistanceContent>? AssistancePresented;

	public event Action? AssistanceCleared;

	public void ShowAssistance(TutorialAssistanceContent content)
	{
		if (!IsActive)
			throw new InvalidOperationException("Cannot show assistance without an active tutorial.");
		AssistancePresented?.Invoke(content);
	}

	public void ClearAssistance() => AssistanceCleared?.Invoke();

	public void CancelActive()
	{
		if (_activeFlow is null)
			return;

		_activeFlow = null;
		_state.ActiveFlowId = null;
		_state.ActiveStepIndex = -1;
		AssistanceCleared?.Invoke();
	}

	public void NotifyAssistanceRequested() => AssistanceRequested?.Invoke();

	public void NotifyDeliveryContractCompleted(string contractId)
	{
		if (_progress.IsCompleted(TutorialGraduation.Id))
			return;

		if (_state.BeatBContractId is not { } beatBId || contractId != beatBId)
			return;

		if (!_orchestrator.Map.ContractRegistry.TryGet(contractId, out var contract)
			|| contract.Objective is not DeliveryObjective)
			return;

		_runContext.PendingTutorialGraduation = true;
		_state.CurrentBeat = TutorialBeat.Graduation;
	}

	public void NotifyFlowCompleted(TutorialFlow flow)
	{
		if (flow.Id == TutorialGraduation.Id)
			_runContext.PendingTutorialGraduation = false;
	}

	private void CompleteActiveFlow()
	{
		var flow = _activeFlow
			?? throw new InvalidOperationException("Cannot complete a tutorial without an active flow.");

		_progress.Complete(flow.Id);
		_activeFlow = null;
		_state.ActiveFlowId = null;
		_state.ActiveStepIndex = -1;
		AssistanceCleared?.Invoke();
		NotifyFlowCompleted(flow);
		FlowCompleted?.Invoke(flow);
	}

	private void RegisterContractObservation()
	{
		_contractCompletionSubscription ??=
			_orchestrator.Subscribe<CompleteContractAction>(OnContractCompleted);
	}

	private void OnContractCompleted(CompleteContractAction action)
	{
		if (action.ActorId != _orchestrator.PlayerId
			|| !_orchestrator.Map.ContractRegistry.TryGet(action.ContractId, out var contract))
			return;

		GameLog.Log(
			$"Tutorial observed completed contract: id='{action.ContractId}', "
			+ $"objective='{contract.Objective.GetType().Name}'.");
		if (contract.Objective is DeliveryObjective)
			NotifyDeliveryContractCompleted(action.ContractId);

		ReconcileBeatTransitions();
		ReconcileFlowProgress(cancelBattleFlowWhenOffBattlefield: false);
		PresentGraduationIfPending();
	}

	public void ReconcileBeatTransitions()
	{
		if (_state.BeatBOffered || _state.BeatAContractId is not { } beatAId)
			return;

		var beatACompleted = _orchestrator.Map.ContractRegistry.IsCompleted(beatAId);
		GameLog.Log(
			$"Tutorial Beat B reconciliation: beatAContractId='{beatAId}', "
			+ $"beatACompleted={beatACompleted}.");
		if (!beatACompleted)
			return;

		_state.CurrentBeat = TutorialBeat.BeatBDelivery;
		_state.BeatBContractId = TutorialBeatContracts.OfferBeatB(_orchestrator.Map);
		_orchestrator.Map.StoryObjectives.Add(
			StoryObjective.BeatBContract(_state.BeatBContractId));
		_state.BeatBOffered = true;
		GameLog.Log(
			$"Tutorial Beat B offered: beatAContractId='{beatAId}', "
			+ $"beatBContractId='{_state.BeatBContractId}'.");
	}

	private void ReconcileFlowProgress(bool cancelBattleFlowWhenOffBattlefield)
	{
		if (IsFirstContractBeatSatisfied())
			EnsureFlowCompleted(StoryObjective.FirstContractId);

		if (IsFirstBattleTutorialSatisfied())
			EnsureFlowCompleted(FirstBattleTutorial.Id);
		else if (cancelBattleFlowWhenOffBattlefield
			&& _activeFlow?.Id == FirstBattleTutorial.Id)
			CancelActive();
	}

	private bool IsFirstContractBeatSatisfied() =>
		!_orchestrator.Map.StoryObjectives.Active.Any(
			objective => objective.Id == StoryObjective.FirstContractId);

	private bool IsFirstBattleTutorialSatisfied() =>
		_state.BeatAContractId is { } beatAId
		&& _orchestrator.Map.ContractRegistry.IsCompleted(beatAId);

	private void EnsureFlowCompleted(string flowId)
	{
		if (_progress.IsCompleted(flowId))
		{
			if (_activeFlow?.Id == flowId)
				CancelActive();
			return;
		}

		if (_activeFlow?.Id == flowId)
		{
			CompleteActiveFlow();
			return;
		}

		_progress.Complete(flowId);
	}

	private static void ValidateFlow(TutorialFlow flow)
	{
		ArgumentException.ThrowIfNullOrEmpty(flow.Id);
		ArgumentNullException.ThrowIfNull(flow.Steps);
		if (flow.Steps.Count == 0)
			throw new ArgumentException("Tutorial flow must contain at least one step.", nameof(flow));
		foreach (var candidate in flow.Steps)
		{
			ArgumentNullException.ThrowIfNull(candidate);
			if (candidate.TargetId is not null)
				ArgumentException.ThrowIfNullOrEmpty(candidate.TargetId);
			ArgumentNullException.ThrowIfNull(candidate.Dialog);
			if (candidate.AdvanceOnAccept)
				ArgumentException.ThrowIfNullOrEmpty(candidate.Dialog.AcceptText);
			else if (candidate.Dialog.AcceptText is not null)
				throw new ArgumentException(
					"Externally advanced tutorial steps cannot show an accept button.",
					nameof(flow));
		}
	}

	public void Dispose()
	{
		_contractCompletionSubscription?.Dispose();
		_narrativeSubscription?.Dispose();
		CancelActive();
	}
}
