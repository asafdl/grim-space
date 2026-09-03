using GrimSpace.World.StarSystem.Areas;

namespace GrimSpace.World.StarSystem.Contracts.Objectives;

public sealed record SpawnEncounterGroup(
	string GroupId,
	AreaPick SearchArea,
	int RequiredCount,
	FleetSpawnSpec Spawn)
	: ISpawnEncounterGroup;
