namespace GrimSpace.World.StarSystem.Contracts.Objectives;

public sealed record HuntObjective(IReadOnlyList<SpawnEncounterGroup> SpawnGroups)
	: IContractObjective, IHasSpawnGroups
{
	IReadOnlyList<ISpawnEncounterGroup> IHasSpawnGroups.SpawnGroups => SpawnGroups;
}
