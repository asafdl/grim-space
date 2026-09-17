namespace GrimSpace.World.StarSystem.Units;

public sealed record Engagement(
    string Id,
	EEngagementPhase Phase,
	string InitiatorFleetId,
	HashSet<string> EngagementParticipantIds,
	string? HuntedBy,
	string? Hunting);
