using GrimSpace.Run;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Events;

/// <summary>Context checks a roll action opts into; each event decides what a failed check means.</summary>
public static class WorldEventContexts
{
	/// <summary>Active pilots that have a ship to blame events on.</summary>
	public static IEnumerable<Pilot> FieldedPilots(StarMap world) =>
		world.PilotRegistry?.Active.Where(pilot => !string.IsNullOrEmpty(pilot.AssignedShipId)) ?? [];

	public static bool ShipIsTraveling(StarMap world, string shipId) =>
		world.FleetRegistry.TryFleetContainingMember(shipId, out var fleet)
		&& fleet.State.Travel is FleetTravel.Journey;

	/// <summary>No other prompt is blocking the player and no world event is already pending.</summary>
	public static bool CanOpenPrompt(StarMap world) =>
		!world.WaitingForPlayerInput && world.PendingWorldEvent is null;
}
