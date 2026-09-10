namespace GrimSpace.Education;

public sealed record TutorialDialogContent(
	string Title,
	string Message,
	string AcceptText = "Accept");

public interface ITutorialDialog
{
	event Action? Accepted;

	bool IsOpen { get; }

	void Open(TutorialDialogContent content);

	void Close();
}
