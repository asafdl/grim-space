namespace GrimSpace.Run.Persistence;

public sealed record TutorialStateDto(
	string? BeatAContractId,
	string? BeatBContractId,
	int ActiveStepIndex,
	bool PendingTutorialGraduation,
	IReadOnlyList<string> CompletedFlows);
