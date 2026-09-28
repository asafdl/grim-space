using GrimSpace.Education;
using GrimSpace.Tutorials;

namespace GrimSpace.Tests.Tutorials;

internal static class TutorialTestFlows
{
	public static TutorialFlow MinimalFirstBattleFlow() =>
		new(
			FirstBattleTutorial.Id,
			[
				new TutorialStep(
					FirstBattleTutorial.Turn1MoveTargetId,
					new TutorialDialogContent("Test", "Test body", AcceptText: null),
					ShowIndicator: false,
					FocusTarget: false,
					AdvanceOnAccept: false),
			]);
}

/// <summary>Godot-free stand-in for <see cref="TutorialPresentationBinding"/> in controller-only tests.</summary>
internal sealed class TutorialDialogTestBinding : IDisposable
{
	private readonly TutorialController _controller;
	private readonly ITutorialDialog _dialog;

	public TutorialDialogTestBinding(TutorialController controller, ITutorialDialog dialog)
	{
		_controller = controller;
		_dialog = dialog;
		_dialog.Accepted += OnAccepted;
		_controller.StepPresented += OnStepPresented;
		_controller.FlowCompleted += OnFlowCompleted;
	}

	public void Attach() => _controller.RepresentActiveStep();

	public void Dispose()
	{
		_dialog.Accepted -= OnAccepted;
		_controller.StepPresented -= OnStepPresented;
		_controller.FlowCompleted -= OnFlowCompleted;
		_dialog.Close();
	}

	private void OnFlowCompleted(TutorialFlow _) => _dialog.Close();

	private void OnStepPresented(TutorialFlow _, TutorialStep step, bool openDialog)
	{
		if (openDialog)
			_dialog.Open(step.Dialog);
		else
			_dialog.Close();
	}

	private void OnAccepted()
	{
		if (_controller.ActiveStep is { AdvanceOnAccept: true })
			_controller.AdvanceActive();
	}
}

internal sealed class TestDialog : ITutorialDialog
{
	public event Action? Accepted;

	public event Action? AssistanceRequested;

	public event Action<string>? WorldLinkClicked;

	public TutorialDialogContent? Content { get; private set; }

	public bool IsOpen => Content is not null;

	public void Open(TutorialDialogContent content) => Content = content;

	public void Close() => Content = null;

	public void ShowAssistance(TutorialAssistanceContent content) { }

	public void ClearAssistance() { }

	public void SimulateAccept() => Accepted?.Invoke();
}

internal sealed class TestWorldFocus : IWorldFocus
{
	public WorldFocusResult Focus(string objectId) =>
		new WorldFocusResult.Accepted(new TestHandle());
}

internal sealed class TestWorldIndicator : IWorldIndicator
{
	public WorldIndicatorResult Show(string objectId) =>
		new WorldIndicatorResult.Shown(new TestHandle());
}

internal sealed class TestHandle : IWorldFocusHandle, IWorldIndicatorHandle
{
	public void Dispose() { }
}
