using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Contracts;

public static class ContractFleetPlacement
{
	public sealed record PlannedSpawn(
		string GroupId,
		string UnitId,
		Coord Coord,
		FleetSpawnSpec Spawn);

	public static IReadOnlyList<PlannedSpawn> Plan(
		IReadOnlyList<ISpawnEncounterGroup> spawnGroups,
		string contractId,
		StarMap map,
		FleetRegistry existingUnits)
	{
		ArgumentException.ThrowIfNullOrEmpty(contractId);
		ArgumentNullException.ThrowIfNull(spawnGroups);
		ArgumentNullException.ThrowIfNull(map);
		ArgumentNullException.ThrowIfNull(existingUnits);

		var planned = new List<PlannedSpawn>();
		var plannedIds = new HashSet<string>(StringComparer.Ordinal);

		foreach (var group in spawnGroups)
		{
			ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(group.RequiredCount, 0);
			if (group.SearchArea.SpawnPoints.Count < group.RequiredCount)
			{
				throw new InvalidOperationException(
					$"Search area for group '{group.GroupId}' has {group.SearchArea.SpawnPoints.Count} spawn points, "
					+ $"but {group.RequiredCount} are required.");
			}

			for (var index = 0; index < group.RequiredCount; index++)
			{
				var unitId = $"{contractId}.{group.GroupId}.{index}";
				if (!plannedIds.Add(unitId))
				{
					throw new InvalidOperationException(
						$"Duplicate planned unit id '{unitId}' for contract '{contractId}'.");
				}

				if (existingUnits.Contains(unitId))
				{
					throw new InvalidOperationException(
						$"Fleet id '{unitId}' already exists in the registry.");
				}

				var coord = group.SearchArea.SpawnPoints[index];
				planned.Add(new PlannedSpawn(group.GroupId, unitId, coord, group.Spawn));
			}
		}

		return planned;
	}

}
