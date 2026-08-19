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
		Assert.Equal(StarMap.DevGridWidth, world.Width);
		Assert.Equal(StarMap.DevGridHeight, world.Height);
		Assert.Equal(4, world.PointsOfInterest.Count);

		Assert.StartsWith("star-", world.PointsOfInterest[0].Id);
		Assert.StartsWith("planet-", world.PointsOfInterest[1].Id);
		Assert.StartsWith("planet-", world.PointsOfInterest[2].Id);
		Assert.StartsWith("station-", world.PointsOfInterest[3].Id);

		var seen = new HashSet<Coord>();
		foreach (var poi in world.PointsOfInterest)
		{
			Assert.NotEmpty(poi.Cells);
			foreach (var cell in poi.Cells)
			{
				Assert.True(world.IsInBounds(cell), $"Out of bounds: {cell}");
				Assert.True(seen.Add(cell), $"Overlapping cell: {cell}");
			}
		}
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
		Assert.Equal(3, fork.Timeline.Clock.Current);

		fork.Timeline.Clock.Set(9);
		Assert.Equal(3, world.Timeline.Clock.Current);
		Assert.Equal(9, fork.Timeline.Clock.Current);
	}
}

