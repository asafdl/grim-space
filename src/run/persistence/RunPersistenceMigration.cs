using GrimSpace.Run.Items;
using GrimSpace.World.StarSystem;

namespace GrimSpace.Run.Persistence;

internal static class RunPersistenceMigration
{
	internal static void ApplyLegacyShipOwners(
		Inventory inventory,
		StarMap map,
		RunPilotRegistry pilots)
	{
		ArgumentNullException.ThrowIfNull(inventory);
		ArgumentNullException.ThrowIfNull(map);
		ArgumentNullException.ThrowIfNull(pilots);

		foreach (var ship in inventory.All)
		{
			if (!inventory.IsPlayerOwned(ship.Id))
				continue;

			if (pilots.TryGetForShip(ship.Id, out _))
				continue;

			if (!map.FleetRegistry.TryFleetContainingMember(ship.Id, out var fleet))
				continue;

			if (string.Equals(fleet.State.Id, State.PlayerFleetUnitId, StringComparison.Ordinal))
				continue;

			inventory.ReassignOwner(ship.Id, fleet.State.Id);
		}
	}
}
