using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.FleetSpawner;

public static class FleetSpawnerQueries
{
	public static int CountTowardTarget(
		FleetRegistry registry,
		EFleetSpawnerSource source,
		int tick) =>
		registry.All.Count(fleet =>
			fleet.State.SpawnerSource == source
			&& (fleet.State.FleetSpawnerExpiresAtTick > tick
				|| (fleet.State.FleetSpawnerExpiresAtTick <= tick
					&& fleet.State.CurrentEngagement is not null)));

	public static IReadOnlyList<string> DueForExpiry(
		FleetRegistry registry,
		EFleetSpawnerSource source,
		int tick) =>
		registry.All
			.Where(fleet =>
				fleet.State.SpawnerSource == source
				&& fleet.State.FleetSpawnerExpiresAtTick <= tick
				&& fleet.State.CurrentEngagement is null)
			.Select(fleet => fleet.State.Id)
			.ToArray();
}
