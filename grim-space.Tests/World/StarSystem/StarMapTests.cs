using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Poi;

namespace GrimSpace.Tests.World.StarSystem;

public sealed class StarMapTests
{
	[Fact]
	public void CreateDevDefault_HasFiveNonOverlappingInBoundsPois()
	{
		var world = StarMap.CreateDevDefault(42);

		Assert.Equal(42, world.Seed);
		Assert.Equal(StarMap.DevMapWidth, world.Width);
		Assert.Equal(StarMap.DevMapHeight, world.Height);
		Assert.Equal(5, world.PointsOfInterest.Count);
		Assert.Equal(EStarSystemClass.Supply, world.Blueprint.SystemClass);

		Assert.Contains(world.PointsOfInterest, poi => poi.Kind == EPointOfInterestKind.Star);
		Assert.Contains(world.PointsOfInterest, poi => poi.Kind == EPointOfInterestKind.AsteroidField);
		Assert.Contains(world.PointsOfInterest, poi => poi.Kind == EPointOfInterestKind.Planet);
		Assert.Contains(world.PointsOfInterest, poi => poi.Kind == EPointOfInterestKind.Station);
		Assert.Contains(world.PointsOfInterest, poi => poi.Kind == EPointOfInterestKind.Wormhole);

		for (var i = 0; i < world.PointsOfInterest.Count; i++)
		{
			for (var j = i + 1; j < world.PointsOfInterest.Count; j++)
				Assert.False(
					StarMap.PoisOverlap(world.PointsOfInterest[i], world.PointsOfInterest[j]),
					$"POIs overlap: {world.PointsOfInterest[i].Id} and {world.PointsOfInterest[j].Id}");
		}

		foreach (var poi in world.PointsOfInterest)
		{
			Assert.True(world.IsInBounds(poi.Center), $"Center out of bounds: {poi.Center}");
			Assert.True(poi.Radius > 0);

			Assert.True(GridBounds.IsCircleWhollyInRectangle(poi.Center, poi.Radius, world.Width, world.Height),
				$"Radius extends out of bounds for {poi.Id}");
		}
	}

	[Fact]
	public void IsInBounds_AcceptsMaxPoint_RejectsOverflow()
	{
		var world = StarMap.CreateDevDefault();

		Assert.True(world.IsInBounds(new Coord(1023, 0, 1023)));
		Assert.False(world.IsInBounds(new Coord(1024, 0, 0)));
		Assert.False(world.IsInBounds(new Coord(0, 1, 0)));
	}

	[Fact]
	public void Fork_PreservesStateAndIndependentsTimeline()
	{
		var world = StarMap.CreateDevDefault(7);
		world.Timeline.Clock.Set(3);

		var fork = world.Fork();

		Assert.Equal(world.Seed, fork.Seed);
		Assert.Equal(world.Width, fork.Width);
		Assert.Equal(world.Height, fork.Height);
		Assert.Same(world.Blueprint, fork.Blueprint);
		Assert.Same(world.PointsOfInterest, fork.PointsOfInterest);
		Assert.Same(world.DocksById, fork.DocksById);
		Assert.Same(world.DocksByPoiId, fork.DocksByPoiId);
		Assert.Same(world.RoutesById, fork.RoutesById);
		Assert.NotSame(world.Timeline, fork.Timeline);
		Assert.NotSame(world.TrafficController, fork.TrafficController);
		Assert.NotSame(world.UnitRegistry, fork.UnitRegistry);
		Assert.Equal(3, fork.Timeline.Clock.Current);
		Assert.Equal(4, world.UnitRegistry.Ids.Count());
		Assert.Equal(4, fork.UnitRegistry.Ids.Count());

		fork.Timeline.Clock.Set(9);
		fork.UnitRegistry.UnitOf(SupplySystemGenerator.MinerOneId).State.AdvanceTransit(99);
		Assert.Equal(3, world.Timeline.Clock.Current);
		Assert.Equal(9, fork.Timeline.Clock.Current);
		Assert.NotEqual(
			world.UnitRegistry.UnitOf(SupplySystemGenerator.MinerOneId).State.Journey.LongitudinalProgress,
			fork.UnitRegistry.UnitOf(SupplySystemGenerator.MinerOneId).State.Journey.LongitudinalProgress);
	}

	[Fact]
	public void CreateDevDefault_HasFourDocks_NoStarDock()
	{
		var world = StarMap.CreateDevDefault(0);

		Assert.Equal(4, world.DocksById.Count);
		Assert.Equal(4, world.UnitRegistry.Ids.Count());
		Assert.DoesNotContain(
			world.PointsOfInterest.First(p => p.Kind == EPointOfInterestKind.Star).Id,
			world.DocksByPoiId.Keys);
	}
}
