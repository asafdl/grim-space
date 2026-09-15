namespace GrimSpace.Education;

public sealed record TutorialDialogContent(
	string Title,
	string Message,
	string? AcceptText = "Accept");

public sealed record TutorialAssistanceContent(
	string Message,
	string ActionText);

public interface ITutorialDialog
{
	event Action? Accepted;

	event Action? AssistanceRequested;

	event Action<string>? WorldLinkClicked;

	bool IsOpen { get; }

	void Open(TutorialDialogContent content);

	void ShowAssistance(TutorialAssistanceContent content);

	void ClearAssistance();

	void Close();
}
