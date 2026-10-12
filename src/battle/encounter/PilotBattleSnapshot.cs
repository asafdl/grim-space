using GrimSpace.Pilots;

namespace GrimSpace.Battle.Encounter;

public sealed record PilotBattleSnapshot(
	string PilotId,
	IReadOnlyList<EPilotSkillId> SkillIds);
