using GrimSpace.Pilots;

namespace GrimSpace.Run.Persistence;

public sealed record PilotBattleSnapshotDto(
	string PilotId,
	IReadOnlyList<EPilotSkillId> SkillIds);
