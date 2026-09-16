using GrimSpace.Math.Grid;
using GrimSpace.Math.Routes;

namespace GrimSpace.Tests.Math.Routes;

public sealed class PiecewiseRouteSamplerTests
{
	[Fact]
	public void TimeRequired_SumsSegmentTravelTimes()
	{
		var segments = new[]
		{
			new RouteSegment(
				[new Coord(0, 0, 0), new Coord(10, 0, 0)],
				10,
				1.0),
			new RouteSegment(
				[new Coord(10, 0, 0), new Coord(10, 0, 10)],
				10,
				2.0),
		};

		Assert.Equal(15, PiecewiseRouteSampler.TimeRequired(segments, baseSpeed: 1));
	}

	[Fact]
	public void SampleAtElapsed_InterpolatesAcrossSegments()
	{
		var segments = new[]
		{
			new RouteSegment(
				[new Coord(0, 0, 0), new Coord(10, 0, 0)],
				10,
				1.0),
			new RouteSegment(
				[new Coord(10, 0, 0), new Coord(10, 0, 10)],
				10,
				1.0),
		};

		var (midFirst, _) = PiecewiseRouteSampler.SampleAtElapsed(segments, 5, baseSpeed: 1);
		var (startSecond, _) = PiecewiseRouteSampler.SampleAtElapsed(segments, 10, baseSpeed: 1);
		var (midSecond, _) = PiecewiseRouteSampler.SampleAtElapsed(segments, 15, baseSpeed: 1);

		Assert.Equal(new Coord(5, 0, 0), midFirst);
		Assert.Equal(new Coord(10, 0, 0), startSecond);
		Assert.Equal(new Coord(10, 0, 5), midSecond);
	}

	[Fact]
	public void SampleContinuousAtElapsed_UsesCorrectTangentAcrossLegBoundaries()
	{
		var segments = new[]
		{
			new RouteSegment(
				[new Coord(0, 0, 0), new Coord(10, 0, 0)],
				10,
				1.0),
			new RouteSegment(
				[new Coord(10, 0, 0), new Coord(10, 0, 10)],
				10,
				1.0),
		};

		var endOfFirst = PiecewiseRouteSampler.SampleContinuousAtElapsed(segments, 9.5, baseSpeed: 1);
		var midSecond = PiecewiseRouteSampler.SampleContinuousAtElapsed(segments, 15, baseSpeed: 1);

		Assert.Equal(0, endOfFirst.SegmentIndex);
		Assert.Equal(9.5, endOfFirst.Route.X);
		Assert.Equal(0, endOfFirst.Route.Z);
		Assert.Equal(0, endOfFirst.Route.TangentZ, 3);
		Assert.True(endOfFirst.Route.TangentX > 0.9);

		Assert.Equal(1, midSecond.SegmentIndex);
		Assert.Equal(10, midSecond.Route.X);
		Assert.Equal(5, midSecond.Route.Z);
		Assert.Equal(0, midSecond.Route.TangentX, 3);
		Assert.True(midSecond.Route.TangentZ > 0.9);
	}
}
