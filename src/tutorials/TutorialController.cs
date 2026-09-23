using GrimSpace.Core.Log;
using GrimSpace.Education;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Narrative;
using GrimSpace.World.StarSystem.Objectives;

namespace GrimSpace.Tutorials;

public sealed class TutorialController : IDisposable
{
	public const string GraduationFlowId = "tutorial-graduation";

	private readonly StarSystemOrchestrator _orchestrator;
	private readonly TutorialState _state;
	private IDisposable? _contractCompletionSubscription;
	private IDisposable? _narrativeCompletionSubscription;
	private TutorialFlow? _activeFlow;

	public TutorialController(StarSystemOrchestrator orchestrator, TutorialState state)
	{
		_orchestrator = orchestrator;
		_state = state;
	}

	public TutorialState State => _state;

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
		if (_state.BeatAContractId is not null)
			return;

		_state.BeatAContractId = TutorialBeatContracts.OfferBeatA(_orchestrator.Map);
		GameLog.Log($"Tutorial initialized: beatAContractId='{_state.BeatAContractId}'.");
		EnsureContractObservation();
		ReconcileFirstContractStoryObjective();
		ReconcileBeatTransitions();
	}

	public void EnsureContractObservation()
	{
		_contractCompletionSubscription ??=
			_orchestrator.Subscribe<CompleteContractAction>(OnContractCompleted);
		_narrativeCompletionSubscription ??=
			_orchestrator.Subscribe<CompleteNarrativeAction>(OnNarrativeCompleted);
	}

	public void ReconcileFromWorldState(bool cancelBattleFlowWhenOffBattlefield)
	{
		ReconcileBeatTransitions();
		ReconcileFlowProgress(cancelBattleFlowWhenOffBattlefield);
	}

	public void PresentGraduationIfPending()
	{
		ReconcileFlowProgress(cancelBattleFlowWhenOffBattlefield: true);

		if (!_state.PendingTutorialGraduation || _state.IsFlowCompleted(GraduationFlowId) || IsActive)
		{
			if (_state.IsFlowCompleted(GraduationFlowId))
				_state.PendingTutorialGraduation = false;
			return;
		}

		TryStartFlow(CreateGraduationFlow());
	}

	public bool TryStartFlow(TutorialFlow flow)
	{
		if (_state.IsFlowCompleted(flow.Id) || _activeFlow is not null)
			return false;

		_activeFlow = flow;
		_state.ActiveStepIndex = 0;
		RepresentActiveStep();
		return true;
	}

	public void RepresentActiveStep(bool openDialog = true)
	{
		if (_activeFlow is null || ActiveStep is not { } step)
			return;

		StepPresented?.Invoke(_activeFlow, step, openDialog);
	}

	public void AdvanceActive() => AdvanceActive(clearDialogBeforeNext: false);

	public void AdvanceActiveSilently() => AdvanceActive(clearDialogBeforeNext: true);

	private void AdvanceActive(bool clearDialogBeforeNext)
	{
		if (_activeFlow is not { } flow)
			return;

		var nextStepIndex = _state.ActiveStepIndex + 1;
		if (nextStepIndex >= flow.Steps.Count)
		{
			CompleteActiveFlow();
			return;
		}

		_state.ActiveStepIndex = nextStepIndex;
		if (clearDialogBeforeNext)
			StepPresented?.Invoke(flow, flow.Steps[nextStepIndex], false);
		else
			RepresentActiveStep(openDialog: true);
	}

	public event Action<TutorialAssistanceContent>? AssistancePresented;

	public event Action? AssistanceCleared;

	public void ShowAssistance(TutorialAssistanceContent content) =>
		AssistancePresented?.Invoke(content);

	public void ClearAssistance() => AssistanceCleared?.Invoke();

	public void CancelActive()
	{
		if (_activeFlow is null)
			return;

		_activeFlow = null;
		_state.ActiveStepIndex = -1;
		AssistanceCleared?.Invoke();
	}

	public void NotifyAssistanceRequested() => AssistanceRequested?.Invoke();

	internal void NotifyDeliveryContractCompleted(string contractId)
	{
		if (_state.IsFlowCompleted(GraduationFlowId))
			return;

		if (_state.BeatBContractId is not { } beatBId || contractId != beatBId)
			return;

		if (!_orchestrator.Map.ContractRegistry.TryGet(contractId, out var contract)
			|| contract.Objective is not DeliveryObjective)
			return;

		_state.PendingTutorialGraduation = true;
	}

	private void CompleteActiveFlow()
	{
		var flow = _activeFlow!;
		_state.CompleteFlow(flow.Id);
		if (flow.Id == GraduationFlowId)
			_state.PendingTutorialGraduation = false;

		_activeFlow = null;
		_state.ActiveStepIndex = -1;
		AssistanceCleared?.Invoke();
		FlowCompleted?.Invoke(flow);
	}

	private void OnNarrativeCompleted(CompleteNarrativeAction action)
	{
		if (action.NarrativeId != MapNarratives.OpeningId)
			return;

		ReconcileFirstContractStoryObjective();
	}

	private void ReconcileFirstContractStoryObjective()
	{
		var map = _orchestrator.Map;
		if (map.ActiveNarrativeId is not null)
			return;

		if (map.StoryObjectives.Active.Any(objective => objective.Id == StoryObjective.FirstContractId))
			return;

		if (_state.BeatAContractId is { } beatAId && !map.ContractRegistry.IsPending(beatAId))
			return;

		map.StoryObjectives.Add(
			StoryObjective.FirstContract(map.Blueprint.SupplyPlan.AdministrativePoiId));
	}

	private void OnContractCompleted(CompleteContractAction action)
	{
		if (action.ActorId != _orchestrator.PlayerId
			|| !_orchestrator.Map.ContractRegistry.TryGet(action.ContractId, out var contract))
			return;

		if (contract.Objective is DeliveryObjective)
			NotifyDeliveryContractCompleted(action.ContractId);

		ReconcileBeatTransitions();
		ReconcileFlowProgress(cancelBattleFlowWhenOffBattlefield: false);
		PresentGraduationIfPending();
	}

	public void ReconcileBeatTransitions()
	{
		if (_state.BeatBContractId is not null || _state.BeatAContractId is not { } beatAId)
			return;

		if (!_orchestrator.Map.ContractRegistry.IsCompleted(beatAId))
			return;

		_state.BeatBContractId = TutorialBeatContracts.OfferBeatB(_orchestrator.Map);
		_orchestrator.Map.StoryObjectives.Add(
			StoryObjective.BeatBContract(_state.BeatBContractId));
		GameLog.Log(
			$"Tutorial Beat B offered: beatAContractId='{beatAId}', "
			+ $"beatBContractId='{_state.BeatBContractId}'.");
	}

	private void ReconcileFlowProgress(bool cancelBattleFlowWhenOffBattlefield)
	{
		if (_state.BeatAContractId is { } beatAId
			&& _orchestrator.Map.ContractRegistry.IsCompleted(beatAId))
			EnsureFlowCompleted(FirstBattleTutorial.Id);
		else if (cancelBattleFlowWhenOffBattlefield
			&& _activeFlow?.Id == FirstBattleTutorial.Id)
			CancelActive();
	}

	private void EnsureFlowCompleted(string flowId)
	{
		if (_state.IsFlowCompleted(flowId))
		{
			if (_activeFlow?.Id == flowId)
				CancelActive();
			return;
		}

		if (_activeFlow?.Id == flowId)
			CompleteActiveFlow();
		else
			_state.CompleteFlow(flowId);
	}

	private static TutorialFlow CreateGraduationFlow() =>
		new(
			GraduationFlowId,
			[
				new TutorialStep(
					null,
					new TutorialDialogContent(
						"Tutorial complete",
						TutorialCopy.TutorialGraduationMessage,
						AcceptText: "Accept"),
					ShowIndicator: false,
					FocusTarget: false,
					AdvanceOnAccept: true),
			]);

	public void Dispose()
	{
		_contractCompletionSubscription?.Dispose();
		_narrativeCompletionSubscription?.Dispose();
		CancelActive();
	}
}
