using GrimSpace.Math.Grid;

namespace GrimSpace.Math.Routes;

public static class PolylineSampler
{
	public static RouteSample SampleContinuous(IReadOnlyList<Coord> points, double arcLength) =>
		RouteGeometry.SampleAtArcLength(points, arcLength);

	public static (Coord Position, Coord Tangent) Sample(IReadOnlyList<Coord> points, double arcLength)
	{
		var sample = SampleContinuous(points, arcLength);
		return sample.ToRoundedCoord();
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
