using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Encounter;

//TODO: think about carryover HP or state shared between world and battle
public sealed record BattleParticipant(
	string Id,
	ETeam Team);
