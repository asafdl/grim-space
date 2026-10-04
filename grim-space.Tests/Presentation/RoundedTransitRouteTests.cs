using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Presentation.Map;

namespace GrimSpace.Tests.Presentation;

[BattleTestSuite]
public sealed class RoundedTransitRouteTests
{
	[Fact]
	public void For_CachesRouteByTransitPathIdentity()
	{
		var path = Path(
			[new Coord(0, 0, 0), new Coord(10, 0, 0)],
			[1.0, 1.0]);

		Assert.Same(RoundedTransitRoute.For(path), RoundedTransitRoute.For(path));
	}

	[Fact]
	public void SampleAtElapsed_PreservesAuthoritativeTotalDurationAndEndpoints()
	{
		var path = Path(
			[
				new Coord(0, 0, 0),
				new Coord(10, 0, 0),
				new Coord(10, 0, 10),
				new Coord(20, 0, 10),
			],
			[1.0, 1.0, 2.0, 2.0]);
		var route = RoundedTransitRoute.For(path);

		var start = route.SampleAtElapsed(0.0, speedPerTick: 1.0);
		var end = route.SampleAtElapsed(path.TicksRequired(1.0), speedPerTick: 1.0);

		Assert.Equal(0.0, start.X, 6);
		Assert.Equal(0.0, start.Z, 6);
		Assert.Equal(20.0, end.X, 6);
		Assert.Equal(10.0, end.Z, 6);
	}

	[Fact]
	public void SampleAtElapsed_UsesAuthoritativeSpeedMultipliers()
	{
		var path = Path(
			[
				new Coord(0, 0, 0),
				new Coord(10, 0, 0),
				new Coord(10, 0, 10),
				new Coord(20, 0, 10),
			],
			[1.0, 1.0, 2.0, 2.0]);
		var route = RoundedTransitRoute.For(path);

		var beforeFastLeg = route.SampleAtElapsed(20.0, speedPerTick: 1.0);
		var halfwayThroughFastLeg = route.SampleAtElapsed(22.5, speedPerTick: 1.0);

		Assert.True(halfwayThroughFastLeg.X > beforeFastLeg.X);
		Assert.Equal(25.0, path.TicksRequired(1.0), 6);
	}

	[Fact]
	public void RemainingPoints_StartAtCurrentRoundedPositionAndEndAtDestination()
	{
		var path = Path(
			[
				new Coord(0, 0, 0),
				new Coord(10, 0, 0),
				new Coord(10, 0, 10),
			],
			[1.0, 1.0, 1.0]);
		var route = RoundedTransitRoute.For(path);
		var sample = route.SampleAtElapsed(10.0, speedPerTick: 1.0);

		var remaining = route.RemainingPoints(sample);

		Assert.Equal((sample.X, sample.Z), remaining[0]);
		Assert.Equal((10.0, 10.0), remaining[^1]);
	}

	[Fact]
	public void For_AbsorbsMicroCorrectionIntoBroadCourse()
	{
		var path = Path(
			[
				new Coord(0, 0, 0),
				new Coord(10, 0, 0),
				new Coord(10, 0, 2),
				new Coord(20, 0, 2),
			],
			[1.0, 1.0, 1.0, 1.0]);

		var points = RoundedTransitRoute.For(path).Points;

		Assert.Equal(2, points.Count);
		Assert.Equal((0.0, 0.0), points[0]);
		Assert.Equal((20.0, 2.0), points[^1]);
	}

	[Fact]
	public void For_PreservesDetoursLargerThanMicroCorrectionTolerance()
	{
		var path = Path(
			[
				new Coord(0, 0, 0),
				new Coord(10, 0, 0),
				new Coord(10, 0, 10),
				new Coord(20, 0, 10),
			],
			[1.0, 1.0, 1.0, 1.0]);

		var points = RoundedTransitRoute.For(path).Points;

		Assert.True(points.Count > 2);
		Assert.Contains(points, point => point.Z > 2.0 && point.X < 18.0);
	}

	private static TransitPath Path(
		IReadOnlyList<Coord> points,
		IReadOnlyList<double> speedMultipliers) =>
		TransitPath.FromPoints(points, speedMultipliers);
}
