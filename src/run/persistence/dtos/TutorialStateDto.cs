namespace GrimSpace.Run.Persistence;

public sealed record TutorialStateDto(
	string? BeatAContractId,
	string? BeatBContractId,
	string? ActiveFlowId,
	int ActiveStepIndex,
	bool PendingTutorialGraduation,
	IReadOnlyList<string> CompletedFlows);
