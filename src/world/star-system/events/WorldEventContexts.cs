using GrimSpace.Math;
using GrimSpace.Run;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Events;

/// <summary>Context checks a roll action opts into; each event decides what a failed check means.</summary>
public static class WorldEventContexts
{
	/// <summary>Pilots that have a ship to blame events on.</summary>
	public static IEnumerable<Pilot> FieldedPilots(StarMap world) =>
		world.PilotRegistry?.Assigned ?? [];

	/// <summary>Every pilot on the roster, for events that don't need the pilot's own ship.</summary>
	public static IEnumerable<Pilot> RosterPilots(StarMap world) =>
		world.PilotRegistry?.All ?? [];

	public const int MaxRolledPilots = 6;

	/// <summary>Shuffled sample of at most <see cref="MaxRolledPilots"/> pilots, in the order their checks roll.</summary>
	public static List<Pilot> RollOrder(StableRandom random, IEnumerable<Pilot> pilots)
	{
		var order = pilots.ToList();
		random.Shuffle(order);
		if (order.Count > MaxRolledPilots)
			order.RemoveRange(MaxRolledPilots, order.Count - MaxRolledPilots);
		return order;
	}

	/// <summary>A pilot without a ship rides along with the player fleet.</summary>
	public static bool IsTraveling(StarMap world, Pilot pilot)
	{
		var fleet = pilot.AssignedShipId is { } shipId
			? world.FleetRegistry.TryFleetContainingMember(shipId, out var shipFleet) ? shipFleet : null
			: world.FleetRegistry.TryGet(StarSystemActorIds.PlayerFleet, out var playerFleet) ? playerFleet : null;
		return fleet?.State.Travel is FleetTravel.Journey;
	}

	/// <summary>No other prompt is blocking the player and no world event is already pending.</summary>
	public static bool CanOpenPrompt(StarMap world) =>
		!world.WaitingForPlayerInput && world.PendingWorldEvent is null;
}
