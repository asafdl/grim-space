using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.Tests.World.StarSystem;

internal static class FleetTravelTestAssertions
{
	public static FleetTravel.AtRest AtRest(this State state) =>
		Assert.IsType<FleetTravel.AtRest>(state.Travel);

	public static FleetTravel.Journey Journey(this State state) =>
		Assert.IsType<FleetTravel.Journey>(state.Travel);

	public static bool HasChoreAtDock(this State state, StarMap map) =>
		state.ChoreDockIds.Count > 0
		&& map.DockAt(state) is not null
		&& !WorkScheduler.HasAssignment(map, state.Id);
}
