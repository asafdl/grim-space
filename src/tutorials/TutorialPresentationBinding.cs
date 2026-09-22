using GrimSpace.Education;
using Godot;

namespace GrimSpace.Tutorials;

public sealed class TutorialPresentationBinding : IDisposable
{
	private readonly TutorialController _controller;
	private readonly ITutorialDialog _dialog;
	private readonly WorldLinkNavigator _worldLinks;
	private TutorialStep? _lastPresentedStep;

	public TutorialPresentationBinding(
		TutorialController controller,
		ITutorialDialog dialog,
		WorldLinkNavigator worldLinks)
	{
		_controller = controller;
		_dialog = dialog;
		_worldLinks = worldLinks;
		_dialog.Accepted += OnAccepted;
		_dialog.AssistanceRequested += OnAssistanceRequested;
		_dialog.WorldLinkClicked += OnWorldLinkClicked;
		_controller.StepPresented += OnStepPresented;
		_controller.FlowCompleted += OnFlowCompleted;
		_controller.AssistancePresented += OnAssistancePresented;
		_controller.AssistanceCleared += OnAssistanceCleared;
	}

	public void Attach() => _controller.RepresentActiveStep();

	public void Detach()
	{
		_dialog.Close();
		_worldLinks.Clear();
		_lastPresentedStep = null;
	}

	private void OnFlowCompleted(TutorialFlow _)
	{
		_dialog.Close();
		_worldLinks.Clear();
		_lastPresentedStep = null;
	}

	private void OnStepPresented(TutorialFlow flow, TutorialStep step, bool openDialog)
	{
		if (_lastPresentedStep is
			{
				TargetId: not null,
				FocusTarget: true,
				RetainFocusAfterStep: false,
			})
			_worldLinks.ClearCurrentFocus();

		_worldLinks.ClearIndicator();
		var navigation = PresentTarget(step);
		if (navigation is WorldLinkNavigationResult.FocusFailed or WorldLinkNavigationResult.IndicatorFailed)
		{
			GD.PushWarning(
				$"Tutorial '{flow.Id}' step presentation failed: {navigation.GetType().Name}.");
			_controller.CancelActive();
			return;
		}

		if (openDialog)
			_dialog.Open(step.Dialog);
		else
			_dialog.Close();

		_lastPresentedStep = step;
	}

	private void OnAssistancePresented(TutorialAssistanceContent content) =>
		_dialog.ShowAssistance(content);

	private void OnAssistanceCleared() => _dialog.ClearAssistance();

	private void OnAccepted()
	{
		if (_controller.ActiveStep is { AdvanceOnAccept: true })
			_controller.AdvanceActive();
	}

	private void OnAssistanceRequested() => _controller.NotifyAssistanceRequested();

	private void OnWorldLinkClicked(string objectId) =>
		_worldLinks.Follow(objectId);

	private WorldLinkNavigationResult PresentTarget(TutorialStep step)
	{
		if (step.TargetId is not null)
		{
			return _worldLinks.Follow(
				step.TargetId,
				step.FocusTarget,
				step.ShowIndicator);
		}

		_worldLinks.ClearIndicator();
		return new WorldLinkNavigationResult.Followed();
	}

	public void Dispose()
	{
		_dialog.Accepted -= OnAccepted;
		_dialog.AssistanceRequested -= OnAssistanceRequested;
		_dialog.WorldLinkClicked -= OnWorldLinkClicked;
		_controller.StepPresented -= OnStepPresented;
		_controller.FlowCompleted -= OnFlowCompleted;
		_controller.AssistancePresented -= OnAssistancePresented;
		_controller.AssistanceCleared -= OnAssistanceCleared;
		Detach();
		_worldLinks.Dispose();
	}
}
