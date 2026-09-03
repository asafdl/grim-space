namespace GrimSpace.World.StarSystem.Contracts.Objectives;

public interface IHasSpawnGroups
{
	IReadOnlyList<ISpawnEncounterGroup> SpawnGroups { get; }
}
