using GrimSpace.Education;

namespace GrimSpace.Tutorials;

public sealed record TutorialFlow(
	string Id,
	string WorldObjectId,
	TutorialDialogContent Dialog);
