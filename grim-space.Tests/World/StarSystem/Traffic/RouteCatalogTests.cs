using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Traffic;

namespace GrimSpace.Tests.World.StarSystem.Traffic;

public sealed class RouteCatalogTests
{
	[Fact]
	public void Select_IsDeterministicForSameInputs()
	{
		var world = StarMap.CreateDevDefault(42);
		var station = world.PointsOfInterest.First(p => p.Kind == EPointOfInterestKind.Station);

		var first = world.RouteCatalog.Select(42, "actor-1", station.Id, "planet-dev-a");
		var second = world.RouteCatalog.Select(42, "actor-1", station.Id, "planet-dev-a");

		Assert.Equal(first.Id, second.Id);
	}

	[Fact]
	public void Assign_ReservesNothing()
	{
		var world = StarMap.CreateDevDefault(7);
		var route = world.RoutesById.Values.First();
		var assignment = world.RouteCatalog.Assign(route);

		Assert.Equal(route.Id, assignment.RouteId);
		Assert.Equal(0, assignment.NextSegmentIndex);

		foreach (var registry in world.RegistriesByPoiId.Values)
			registry.Validate();
	}
}
