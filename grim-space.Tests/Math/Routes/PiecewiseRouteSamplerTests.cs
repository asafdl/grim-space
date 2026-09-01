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
}
