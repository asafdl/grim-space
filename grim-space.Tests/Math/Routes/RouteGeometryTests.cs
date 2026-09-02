using GrimSpace.Math.Grid;
using GrimSpace.Math.Routes;

namespace GrimSpace.Tests.Math.Routes;

public sealed class RouteGeometryTests
{
	[Fact]
	public void PointToPolylineDistance_OnSegment_ReturnsZero()
	{
		var polyline = new[]
		{
			new Coord(0, 0, 0),
			new Coord(10, 0, 0),
		};

		Assert.Equal(0, RouteGeometry.PointToPolylineDistance(new Coord(5, 0, 0), polyline));
	}

	[Fact]
	public void PointToPolylineDistance_OffSegment_ReturnsPerpendicularDistance()
	{
		var polyline = new[]
		{
			new Coord(0, 0, 0),
			new Coord(10, 0, 0),
		};

		Assert.Equal(3, RouteGeometry.PointToPolylineDistance(new Coord(5, 0, 3), polyline));
	}

	[Fact]
	public void PointToPolylineDistance_MultiSegment_UsesClosestSegment()
	{
		var polyline = new[]
		{
			new Coord(0, 0, 0),
			new Coord(10, 0, 0),
			new Coord(10, 0, 10),
		};

		Assert.Equal(2, RouteGeometry.PointToPolylineDistance(new Coord(12, 0, 2), polyline));
	}

	[Fact]
	public void PointToPolylineDistance_EmptyPolyline_ReturnsInfinity()
	{
		Assert.Equal(double.PositiveInfinity, RouteGeometry.PointToPolylineDistance(new Coord(0, 0, 0), []));
	}

	[Fact]
	public void PointToPolylineDistance_SinglePoint_ReturnsPointDistance()
	{
		var point = new Coord(3, 0, 4);
		Assert.Equal(5, RouteGeometry.PointToPolylineDistance(point, [new Coord(0, 0, 0)]));
	}
}
