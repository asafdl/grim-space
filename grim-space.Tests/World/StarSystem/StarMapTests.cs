using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem;

public sealed class StarMapTests
{
	[Fact]
	public void CreateDevDefault_HasFourNonOverlappingInBoundsPois()
	{
		var world = StarMap.CreateDevDefault(42);

		Assert.Equal(42, world.Seed);
		Assert.Equal(StarMap.DevMapWidth, world.Width);
		Assert.Equal(StarMap.DevMapHeight, world.Height);
		Assert.Equal(4, world.PointsOfInterest.Count);

		Assert.Equal(EPointOfInterestKind.Star, world.PointsOfInterest[0].Kind);
		Assert.Equal(EPointOfInterestKind.Planet, world.PointsOfInterest[1].Kind);
		Assert.Equal(EPointOfInterestKind.Planet, world.PointsOfInterest[2].Kind);
		Assert.Equal(EPointOfInterestKind.Station, world.PointsOfInterest[3].Kind);

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

			var edgePoints = new[]
			{
				poi.Center + new Coord(poi.Radius, 0, 0),
				poi.Center + new Coord(-poi.Radius, 0, 0),
				poi.Center + new Coord(0, 0, poi.Radius),
				poi.Center + new Coord(0, 0, -poi.Radius),
			};

			foreach (var edge in edgePoints)
				Assert.True(world.IsInBounds(edge), $"Radius extends out of bounds: {edge} for {poi.Id}");
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
		Assert.Same(world.PointsOfInterest, fork.PointsOfInterest);
		Assert.Same(world.RouteCatalog, fork.RouteCatalog);
		Assert.Same(world.DocksById, fork.DocksById);
		Assert.Same(world.SegmentsById, fork.SegmentsById);
		Assert.Same(world.RoutesById, fork.RoutesById);
		Assert.NotSame(world.RegistriesByPoiId, fork.RegistriesByPoiId);
		Assert.NotSame(world.DockStateByDockId, fork.DockStateByDockId);
		Assert.Equal(3, fork.Timeline.Clock.Current);

		fork.Timeline.Clock.Set(9);
		Assert.Equal(3, world.Timeline.Clock.Current);
		Assert.Equal(9, fork.Timeline.Clock.Current);
	}

	[Fact]
	public void CreateDevDefault_HasThreeDocks_NoStarDock()
	{
		var world = StarMap.CreateDevDefault(0);

		Assert.Equal(3, world.DocksById.Count);
		Assert.Equal(3, world.RegistriesByPoiId.Count);
		Assert.DoesNotContain(
			world.PointsOfInterest.First(p => p.Kind == EPointOfInterestKind.Star).Id,
			world.DocksByPoiId.Keys);
	}
}
