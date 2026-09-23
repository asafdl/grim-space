using GrimSpace.Math.Grid;
using GrimSpace.Math.Routes;
using GrimSpace.World.StarSystem.Pathfinding;

namespace GrimSpace.Tests.World.StarSystem.Pathfinding;

[StarSystemTestSuite]
public sealed class TransitPathRemainingTests
{
	[Fact]
	public void RemainingPoints_WithinSegment_IncludesFractionalHeadAndSubsequentPoints()
	{
		var path = TransitPath.FromPoints(
			[new Coord(0, 0, 0), new Coord(10, 0, 0), new Coord(10, 0, 10)],
			[1.0, 1.0, 1.0]);
		var sample = new PiecewiseRouteSample(
			new RouteSample(2.5, 0, 1, 0, 1),
			SegmentIndex: 0);

		var remaining = path.RemainingPoints(sample);

		Assert.Equal(3, remaining.Count);
		Assert.Equal((2.5, 0), remaining[0]);
		Assert.Equal((10, 0), remaining[1]);
		Assert.Equal((10, 10), remaining[2]);
	}

	[Fact]
	public void RemainingPoints_AtLegBoundary_IncludesSubsequentLeg()
	{
		var path = TransitPath.FromPoints(
			[new Coord(0, 0, 0), new Coord(10, 0, 0), new Coord(10, 0, 10)],
			[1.0, 2.0, 2.0]);
		var sample = path.SampleContinuousAtElapsed(10, speedPerTick: 1);

		var remaining = path.RemainingPoints(sample);

		Assert.Equal(2, remaining.Count);
		Assert.Equal((10, 0), remaining[0]);
		Assert.Equal((10, 10), remaining[1]);
	}

	[Fact]
	public void RemainingPoints_AtCompletion_ReturnsOnlyDestination()
	{
		var path = TransitPath.FromPoints(
			[new Coord(0, 0, 0), new Coord(10, 0, 0)],
			[1.0, 1.0]);
		var sample = path.SampleContinuousAtElapsed(10, speedPerTick: 1);

		var remaining = path.RemainingPoints(sample);

		Assert.Equal(2, remaining.Count);
		Assert.Equal((10, 0), remaining[0]);
		Assert.Equal((10, 0), remaining[1]);
	}
}
