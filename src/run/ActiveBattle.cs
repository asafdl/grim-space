using GrimSpace.Battle.Encounter;

namespace GrimSpace.Run;

public sealed class ActiveBattle
{
	public required BattleEncounter Encounter { get; init; }
	public required string InitiatorUnitId { get; init; }
	public required IReadOnlyList<string> ParticipantUnitIds { get; init; }
}
