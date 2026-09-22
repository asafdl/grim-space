using GrimSpace.Education;
using GrimSpace.Core.Log;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;

namespace GrimSpace.Tutorials;

public sealed class MapTutorialAdapter : IDisposable
{
	private readonly TutorialController _controller;
	private readonly StarSystemOrchestrator _orchestrator;
	private TutorialPresentationBinding? _presentation;
	private IDisposable? _moveSubscription;
	public MapTutorialAdapter(TutorialController controller, StarSystemOrchestrator orchestrator)
	{
		_controller = controller ?? throw new ArgumentNullException(nameof(controller));
		_orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
	}

	public void Attach(ITutorialDialog dialog, IWorldFocus worldFocus, IWorldIndicator worldIndicator)
	{
		var state = _controller.State;
		var beatACompleted = state.BeatAContractId is { } beatAId
			&& _orchestrator.Map.ContractRegistry.IsCompleted(beatAId);
		GameLog.Log(
			$"Map tutorial attached: beat='{state.CurrentBeat}', "
			+ $"beatAContractId='{state.BeatAContractId}', beatACompleted={beatACompleted}, "
			+ $"beatBOffered={state.BeatBOffered}.");
		_presentation = new TutorialPresentationBinding(
			_controller,
			dialog,
			new WorldLinkNavigator(worldFocus, worldIndicator));
		_controller.FlowStarted += OnFlowStarted;
		_controller.AttachMapSubscriptions();
		_controller.SyncMapFlows(cancelBattleFlowWhenOffBattlefield: true);
		_controller.PresentGraduationIfPending();
		_presentation.Attach();
		if (_controller.ActiveFlow is { } activeFlow)
			OnFlowStarted(activeFlow);
	}

	public void Detach()
	{
		_controller.FlowStarted -= OnFlowStarted;
		_moveSubscription?.Dispose();
		_moveSubscription = null;
		_controller.DetachMapSubscriptions();
		_presentation?.Detach();
		_presentation?.Dispose();
		_presentation = null;
	}

	private void OnFlowStarted(TutorialFlow flow)
	{
		_moveSubscription?.Dispose();
		_moveSubscription = flow.Id == FirstContractTutorial.Id
			? _orchestrator.Subscribe<MoveAction>(OnMove)
			: null;
	}

	private void OnMove(MoveAction move)
	{
		if (move.ActorId != _orchestrator.PlayerId
			|| move.UnitId != _orchestrator.PlayerId
			|| _controller.ActiveStep?.TargetId is not { } targetId
			|| !_orchestrator.Map.DocksByPoiId.TryGetValue(targetId, out var dock))
			return;

		_controller.NotifyMoveToDock(move.UnitId, targetId, move.Destination, dock.Position);
	}

	public void Dispose() => Detach();
}
