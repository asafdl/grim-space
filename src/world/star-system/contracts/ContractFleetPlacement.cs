using GrimSpace.Math;
using GrimSpace.Math.Grid;
using GrimSpace.Math.Routes;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Contracts;

public static class ContractFleetPlacement
{
	private const int SamplesPerSpawn = 64;

	public sealed record PlannedSpawn(
		string GroupId,
		string UnitId,
		Coord Coord,
		FleetSpawnSpec Spawn);

	public static IReadOnlyList<PlannedSpawn> Plan(
		IReadOnlyList<ISpawnEncounterGroup> spawnGroups,
		string contractId,
		int mapSeed,
		StarMap map,
		UnitRegistry existingUnits)
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
						$"Unit id '{unitId}' already exists in the registry.");
				}

				var coord = SampleCoord(
					map,
					group.SearchArea,
					mapSeed,
					contractId,
					group.GroupId,
					index);
				planned.Add(new PlannedSpawn(group.GroupId, unitId, coord, group.Spawn));
			}
		}

		return planned;
	}

	private static Coord SampleCoord(
		StarMap map,
		AreaPick area,
		int mapSeed,
		string contractId,
		string groupId,
		int index)
	{
		var random = new StableRandom(
			StableSeedMixer.From(mapSeed)
				.Add(contractId)
				.Add(groupId)
				.Add(index)
				.Value);

		for (var sample = 0; sample < SamplesPerSpawn; sample++)
		{
			var angle = random.NextDouble() * System.Math.Tau;
			var radius = System.Math.Sqrt(random.NextDouble()) * area.Radius;
			var x = (int)System.Math.Round(area.Center.X + radius * System.Math.Cos(angle));
			var z = (int)System.Math.Round(area.Center.Z + radius * System.Math.Sin(angle));
			var coord = new Coord(x, 0, z);

			if (!map.IsInBounds(coord))
				continue;

			if (RouteGeometry.Distance(coord, area.Center) > area.Radius)
				continue;

			return coord;
		}

		throw new InvalidOperationException(
			$"Could not place fleet for contract '{contractId}' group '{groupId}' index {index} within search area.");
	}
}
