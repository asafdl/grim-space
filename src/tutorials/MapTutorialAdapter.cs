using GrimSpace.Application;
using GrimSpace.Education;
using GrimSpace.Core.Log;
using GrimSpace.World.StarSystem;

namespace GrimSpace.Tutorials;

public sealed class MapTutorialAdapter : IDisposable
{
	private readonly TutorialController _controller;
	private readonly StarSystemOrchestrator _orchestrator;
	private TutorialPresentationBinding? _presentation;

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
		_controller.FlowCompleted += OnFlowCompleted;
		_controller.AttachMapSubscriptions();
		_controller.SyncMapFlows(cancelBattleFlowWhenOffBattlefield: true);
		_controller.PresentGraduationIfPending();
		_presentation.Attach();
	}

	private void OnFlowCompleted(TutorialFlow flow)
	{
		if (flow.Id == TutorialGraduation.Id)
			GameSettings.SaveShowTutorials(false);
	}

	public void Detach()
	{
		_controller.FlowCompleted -= OnFlowCompleted;
		_controller.DetachMapSubscriptions();
		_presentation?.Detach();
		_presentation?.Dispose();
		_presentation = null;
	}

	public void Dispose() => Detach();
}
