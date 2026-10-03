using GrimSpace.Battle.Objectives;

namespace GrimSpace.Run.Persistence;

public sealed record BattleEncounterSaveDto(
	int Seed,
	string Id,
	EObjective Objective,
	IReadOnlyList<BattleEncounterSpawnDto> Spawns,
	IReadOnlyList<BattleEncounterHazardDto> Hazards);
