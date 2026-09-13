namespace GrimSpace.Battle.Encounter;

public sealed record BattleParticipant(
	string ParticipantId,
	IReadOnlyList<string> TacticalUnitIds);
