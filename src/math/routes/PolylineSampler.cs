using GrimSpace.Math.Grid;

namespace GrimSpace.Math.Routes;

public static class PolylineSampler
{
	public static (Coord Position, Coord Tangent) Sample(IReadOnlyList<Coord> points, double arcLength)
	{
		var (x, z, tangentX, tangentZ) = RouteGeometry.SampleAtArcLength(points, arcLength);
		return (
			new Coord((int)System.Math.Round(x), 0, (int)System.Math.Round(z)),
			new Coord(
				(int)System.Math.Round(tangentX * 1000),
				0,
				(int)System.Math.Round(tangentZ * 1000)));
	}

	public static double Length(IReadOnlyList<Coord> points)
	{
		if (points.Count < 2)
			return 0;

		var total = 0.0;
		for (var i = 1; i < points.Count; i++)
			total += RouteGeometry.Distance(points[i - 1], points[i]);

		return total;
	}
}
