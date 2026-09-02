using GrimSpace.Math.Grid;

namespace GrimSpace.Math.Routes;

public static class RouteGeometry
{
	public static double Distance(Coord a, Coord b)
	{
		var dx = b.X - a.X;
		var dz = b.Z - a.Z;
		return System.Math.Sqrt(dx * (double)dx + dz * (double)dz);
	}

	public static (double X, double Z) UnitVector(double x, double z)
	{
		var length = System.Math.Sqrt(x * x + z * z);
		if (length <= 0.000001)
			return (1.0, 0.0);
		return (x / length, z / length);
	}

	public static double PointToPolylineDistance(Coord point, IReadOnlyList<Coord> polyline)
	{
		if (polyline.Count == 0)
			return double.PositiveInfinity;
		if (polyline.Count == 1)
			return Distance(point, polyline[0]);

		var min = double.PositiveInfinity;
		for (var i = 1; i < polyline.Count; i++)
		{
			var segmentDistance = PointToSegmentDistance(point, polyline[i - 1], polyline[i]);
			if (segmentDistance < min)
				min = segmentDistance;
		}

		return min;
	}

	public static double PointToSegmentDistance(Coord point, Coord start, Coord end)
	{
		var dx = end.X - start.X;
		var dz = end.Z - start.Z;
		var lengthSquared = dx * (double)dx + dz * (double)dz;
		if (lengthSquared <= 0.0)
			return Distance(point, start);

		var t = ((point.X - start.X) * dx + (point.Z - start.Z) * dz) / lengthSquared;
		t = System.Math.Clamp(t, 0.0, 1.0);
		var projectionX = start.X + t * dx;
		var projectionZ = start.Z + t * dz;
		var offsetX = point.X - projectionX;
		var offsetZ = point.Z - projectionZ;
		return System.Math.Sqrt(offsetX * offsetX + offsetZ * offsetZ);
	}

	public static bool SegmentsIntersect(Coord a1, Coord a2, Coord b1, Coord b2)
	{
		var o1 = Orientation(a1, a2, b1);
		var o2 = Orientation(a1, a2, b2);
		var o3 = Orientation(b1, b2, a1);
		var o4 = Orientation(b1, b2, a2);

		if (o1 * o2 < 0.0 && o3 * o4 < 0.0)
			return true;

		const double epsilon = 0.000001;
		return System.Math.Abs(o1) <= epsilon && OnSegment(a1, a2, b1)
			|| System.Math.Abs(o2) <= epsilon && OnSegment(a1, a2, b2)
			|| System.Math.Abs(o3) <= epsilon && OnSegment(b1, b2, a1)
			|| System.Math.Abs(o4) <= epsilon && OnSegment(b1, b2, a2);
	}

	public static (double X, double Z, double TangentX, double TangentZ) SampleAtArcLength(
		IReadOnlyList<Coord> centerline,
		double arcLength)
	{
		if (centerline.Count == 0)
			throw new ArgumentException("Centerline must not be empty.", nameof(centerline));

		if (centerline.Count == 1 || arcLength <= 0)
		{
			var (tangentX, tangentZ) = centerline.Count > 1
				? UnitVector(centerline[1].X - centerline[0].X, centerline[1].Z - centerline[0].Z)
				: (1.0, 0.0);
			return (centerline[0].X, centerline[0].Z, tangentX, tangentZ);
		}

		var remaining = arcLength;
		for (var i = 1; i < centerline.Count; i++)
		{
			var start = centerline[i - 1];
			var end = centerline[i];
			var segmentLength = Distance(start, end);
			if (remaining > segmentLength)
			{
				remaining -= segmentLength;
				continue;
			}

			var t = segmentLength <= 0.0 ? 0.0 : remaining / segmentLength;
			var tangent = UnitVector(end.X - start.X, end.Z - start.Z);
			return (
				start.X + (end.X - start.X) * t,
				start.Z + (end.Z - start.Z) * t,
				tangent.X,
				tangent.Z);
		}

		var last = centerline[^1];
		var previous = centerline[^2];
		var endTangent = UnitVector(last.X - previous.X, last.Z - previous.Z);
		return (last.X, last.Z, endTangent.X, endTangent.Z);
	}

	public static IReadOnlyList<Coord> NormalizePolyline(IReadOnlyList<Coord> points)
	{
		if (points.Count == 0)
			return [];

		var normalized = new List<Coord> { points[0] };
		for (var i = 1; i < points.Count; i++)
		{
			if (points[i] != normalized[^1])
				normalized.Add(points[i]);
		}

		return normalized;
	}

	private static double Orientation(Coord a, Coord b, Coord c) =>
		(b.X - a.X) * (double)(c.Z - a.Z) - (b.Z - a.Z) * (double)(c.X - a.X);

	private static bool OnSegment(Coord a, Coord b, Coord p) =>
		p.X >= System.Math.Min(a.X, b.X) && p.X <= System.Math.Max(a.X, b.X)
		&& p.Z >= System.Math.Min(a.Z, b.Z) && p.Z <= System.Math.Max(a.Z, b.Z);
}
