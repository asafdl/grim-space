namespace GrimSpace.World.StarSystem.Contact;

public sealed record EngagementCommitted(
	string EngagementId,
	string InitiatorFleetId,
	IReadOnlyList<string> ParticipantFleetIds);
