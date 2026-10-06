using GrimSpace.World.StarSystem.Areas;

namespace GrimSpace.World.StarSystem.Contracts.Objectives;

public interface ISpawnEncounterGroup
{
	string GroupId { get; }
	AreaPick SearchArea { get; }
	int RequiredCount { get; }
	FleetSpawnSpec Spawn { get; }
}
