using GrimSpace.Education;

namespace GrimSpace.Tests.Tutorials;

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
