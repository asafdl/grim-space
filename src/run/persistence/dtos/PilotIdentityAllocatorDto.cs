namespace GrimSpace.Run.Persistence;

public sealed record PilotIdentityAllocatorDto(
	IReadOnlyList<string> UsedNames,
	IReadOnlyList<string> UsedNicknames);
