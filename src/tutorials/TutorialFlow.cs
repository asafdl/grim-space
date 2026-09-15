using GrimSpace.Education;

namespace GrimSpace.Tutorials;

public sealed record TutorialStep(
	string? TargetId,
	TutorialDialogContent Dialog,
	bool ShowIndicator = true,
	bool FocusTarget = true,
	bool RetainFocusAfterStep = false,
	bool AdvanceOnAccept = true);

public sealed record TutorialFlow(
	string Id,
	IReadOnlyList<TutorialStep> Steps);
