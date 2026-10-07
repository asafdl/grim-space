using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.FleetSpawner;

public static class OccupancyRules
{
	public static bool IsOccupied(
		StarMap map,
		Coord coord,
		IEnumerable<Coord>? reservedCoords = null,
		string? ignoredFleetId = null)
	{
		if (map.DocksByPosition.ContainsKey(coord)
			|| reservedCoords?.Contains(coord) == true)
			return true;

		foreach (var fleet in map.FleetRegistry.All)
		{
			if (fleet.State.Id == ignoredFleetId)
				continue;
			if (fleet.State.SpawnerSource != EFleetSpawnerSource.None
				&& fleet.State.FleetSpawnerExpiresAtTick <= map.Timeline.Clock.Current
				&& fleet.State.CurrentEngagement is null)
				continue;

			var position = fleet.State.Travel is FleetTravel.Journey journey
				? journey.Destination
				: fleet.State.PositionAt(map, null, 0).Position;
			if (position == coord)
				return true;
		}

		return false;
	}
}
