using GrimSpace.Education;

namespace GrimSpace.Tutorials;

public static class TutorialGraduation
{
	public const string Id = "tutorial-graduation";

	public static TutorialFlow Create() =>
		new(
			Id,
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
}
