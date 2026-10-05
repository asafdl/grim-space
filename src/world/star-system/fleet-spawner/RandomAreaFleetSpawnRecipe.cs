using GrimSpace.Math;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Contracts.Encounter;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.FleetSpawner;

public static class RandomAreaFleetSpawnRecipe
{
	public const int PatrolRadius = 24;

	public static bool TryCreate(
		StarMap map,
		int tick,
		int slot,
		IReadOnlySet<Coord> reservedCoords,
		int expiresAtTick,
		out Fleet fleet)
	{
		var id = FleetIdFor(map, tick, slot);
		var seed = unchecked((int)StableSeedMixer.From(map.Seed).Add(tick).Add(slot).Add("random-area-fleet").Value);
		var areaSeed = StableSeedMixer.From(map.Seed).Add(tick).Add(slot).Add("random-area-area").Value;
		var landmarks = map.NavigationLandmarks.Select(landmark => landmark.Id).OrderBy(id => id, StringComparer.Ordinal).ToArray();
		var args = new AreaPickerArgs(landmarks, DeterministicPickMix: unchecked((long)areaSeed));
		if (!AreaPicker.TryPickWithFallback(map, args, [areaSeed], out var area))
		{
			fleet = null!;
			return false;
		}

		var availablePoints = area.SpawnPoints
			.Where(point => !OccupancyRules.IsOccupied(map, point, reservedCoords))
			.ToArray();
		if (availablePoints.Length == 0)
		{
			fleet = null!;
			return false;
		}
		var coord = availablePoints[0];

		var danger = StarSystemDangerProgression.RollDanger(map, tick, slot, "random-area-fleet-danger");
		var spec = new FleetSpawnSpec(
			EType.PirateFleet,
			EFaction.Pirates,
			seed,
			EncounterBudgetRoller.Roll(map.Seed, id, "random-area-fleet", danger),
			PatrolRadius);
		fleet = MapFleetFactory.CreatePatrolFleet(spec, coord, id, id);
		fleet.State.SpawnerSource = EFleetSpawnerSource.RandomArea;
		fleet.State.FleetSpawnerExpiresAtTick = expiresAtTick;
		return true;
	}

	public static string FleetIdFor(StarMap map, int tick, int slot) =>
		$"fleet-spawn-{StableSeedMixer.From(map.Seed).Add(tick).Add(slot).Add("random-area-fleet").Value:x}";
}
