using GrimSpace.Math.Grid;
using GrimSpace.Math.Routes;

namespace GrimSpace.Tests.Math.Routes;

[BattleTestSuite]
public sealed class RoundedPolylineTests
{
	[Fact]
	public void Constructor_PreservesEndpointsAndRoundsCorner()
	{
		var route = new RoundedPolyline(
			[
				new Coord(0, 0, 0),
				new Coord(10, 0, 0),
				new Coord(10, 0, 10),
			],
			cornerRadius: 2.0,
			cornerSubdivisions: 8);

		Assert.Equal((0.0, 0.0), route.Points[0]);
		Assert.Equal((10.0, 10.0), route.Points[^1]);
		Assert.DoesNotContain((10.0, 0.0), route.Points);
	}

	[Fact]
	public void Constructor_BoundsRoundedCornerByConfiguredRadius()
	{
		const double radius = 2.0;
		var route = new RoundedPolyline(
			[
				new Coord(0, 0, 0),
				new Coord(10, 0, 0),
				new Coord(10, 0, 10),
			],
			radius,
			cornerSubdivisions: 8);

		var cornerPoints = route.Points
			.Where(point => point.X >= 8.0 && point.Z <= 2.0)
			.ToArray();

		Assert.NotEmpty(cornerPoints);
		Assert.All(
			cornerPoints,
			point =>
			{
				var dx = point.X - 10.0;
				var dz = point.Z;
				Assert.True(System.Math.Sqrt(dx * dx + dz * dz) <= radius + 0.000001);
			});
	}

	[Fact]
	public void Constructor_ReplacesAbruptCornerWithGradualDirectionChanges()
	{
		var route = new RoundedPolyline(
			[
				new Coord(0, 0, 0),
				new Coord(10, 0, 0),
				new Coord(10, 0, 10),
			],
			cornerRadius: 2.0,
			cornerSubdivisions: 8);

		for (var i = 2; i < route.Points.Count; i++)
		{
			var previousDirection = Direction(route.Points[i - 2], route.Points[i - 1]);
			var nextDirection = Direction(route.Points[i - 1], route.Points[i]);
			var dot = previousDirection.X * nextDirection.X
				+ previousDirection.Z * nextDirection.Z;
			Assert.True(dot > 0.9, $"Direction changed too sharply at point {i - 1}.");
		}
	}

	[Fact]
	public void SampleNormalized_UsesRoundedArcAndPreservesEndpointProgress()
	{
		var route = new RoundedPolyline(
			[
				new Coord(0, 0, 0),
				new Coord(10, 0, 0),
				new Coord(10, 0, 10),
			],
			cornerRadius: 2.0,
			cornerSubdivisions: 16);

		var start = route.SampleNormalized(0.0);
		var middle = route.SampleNormalized(0.5);
		var end = route.SampleNormalized(1.0);

		Assert.Equal(0.0, start.X, 6);
		Assert.Equal(0.0, start.Z, 6);
		Assert.Equal(9.5, middle.X, 6);
		Assert.Equal(0.5, middle.Z, 6);
		Assert.Equal(10.0, end.X, 6);
		Assert.Equal(10.0, end.Z, 6);
	}

	[Fact]
	public void Constructor_SupportsSinglePointAndRemovesDuplicates()
	{
		var stationary = new RoundedPolyline(
			[new Coord(3, 0, 4)],
			cornerRadius: 2.0,
			cornerSubdivisions: 8);
		var duplicate = new RoundedPolyline(
			[
				new Coord(0, 0, 0),
				new Coord(0, 0, 0),
				new Coord(5, 0, 0),
			],
			cornerRadius: 2.0,
			cornerSubdivisions: 8);

		Assert.Equal((3.0, 4.0), stationary.Points[0]);
		Assert.Equal(0.0, stationary.Length);
		Assert.Equal(2, duplicate.Points.Count);
	}

	[Fact]
	public void Constructor_CollapsesCollinearCellsBeforeRounding()
	{
		var route = new RoundedPolyline(
			[
				new Coord(0, 0, 0),
				new Coord(1, 0, 0),
				new Coord(2, 0, 0),
				new Coord(10, 0, 0),
				new Coord(10, 0, 10),
			],
			cornerRadius: 4.0,
			cornerSubdivisions: 8);

		Assert.Contains((6.0, 0.0), route.Points);
		Assert.DoesNotContain((1.0, 0.0), route.Points);
		Assert.DoesNotContain((2.0, 0.0), route.Points);
	}

	private static (double X, double Z) Direction(
		(double X, double Z) from,
		(double X, double Z) to)
	{
		var dx = to.X - from.X;
		var dz = to.Z - from.Z;
		var length = System.Math.Sqrt(dx * dx + dz * dz);
		return (dx / length, dz / length);
	}
}
