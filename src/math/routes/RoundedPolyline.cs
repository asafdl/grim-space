using GrimSpace.Math.Grid;

namespace GrimSpace.Math.Routes;

public sealed class RoundedPolyline
{
	private const double Epsilon = 0.000001;

	private readonly (double X, double Z)[] _points;
	private readonly double[] _cumulativeLengths;

	public RoundedPolyline(
		IReadOnlyList<Coord> points,
		double cornerRadius,
		int cornerSubdivisions)
	{
		ArgumentNullException.ThrowIfNull(points);
		ArgumentOutOfRangeException.ThrowIfNegative(cornerRadius);
		ArgumentOutOfRangeException.ThrowIfLessThan(cornerSubdivisions, 1);
		if (points.Count == 0)
			throw new ArgumentException("Polyline must not be empty.", nameof(points));

		_points = BuildPoints(
			SimplifyCollinear(RouteGeometry.NormalizePolyline(points)),
			cornerRadius,
			cornerSubdivisions);
		_cumulativeLengths = BuildCumulativeLengths(_points);
	}

	public IReadOnlyList<(double X, double Z)> Points => _points;

	public double Length => _cumulativeLengths[^1];

	public RouteSample SampleNormalized(double progress)
	{
		if (_points.Length == 1 || Length <= Epsilon)
			return new RouteSample(_points[0].X, _points[0].Z, 1.0, 0.0, 0);

		var targetLength = System.Math.Clamp(progress, 0.0, 1.0) * Length;
		var nextPointIndex = FindNextPointIndex(targetLength);
		var start = _points[nextPointIndex - 1];
		var end = _points[nextPointIndex];
		var segmentStartLength = _cumulativeLengths[nextPointIndex - 1];
		var segmentLength = _cumulativeLengths[nextPointIndex] - segmentStartLength;
		var segmentProgress = segmentLength <= Epsilon
			? 0.0
			: (targetLength - segmentStartLength) / segmentLength;
		var tangent = RouteGeometry.UnitVector(end.X - start.X, end.Z - start.Z);

		return new RouteSample(
			start.X + (end.X - start.X) * segmentProgress,
			start.Z + (end.Z - start.Z) * segmentProgress,
			tangent.X,
			tangent.Z,
			nextPointIndex);
	}

	private int FindNextPointIndex(double targetLength)
	{
		if (targetLength <= 0.0)
			return 1;
		if (targetLength >= Length)
			return _points.Length - 1;

		var low = 1;
		var high = _cumulativeLengths.Length - 1;
		while (low < high)
		{
			var middle = low + (high - low) / 2;
			if (_cumulativeLengths[middle] < targetLength)
				low = middle + 1;
			else
				high = middle;
		}

		return low;
	}

	private static (double X, double Z)[] BuildPoints(
		IReadOnlyList<Coord> points,
		double cornerRadius,
		int cornerSubdivisions)
	{
		if (points.Count == 1)
			return [(points[0].X, points[0].Z)];

		var rounded = new List<(double X, double Z)> { (points[0].X, points[0].Z) };
		for (var i = 1; i < points.Count - 1; i++)
		{
			var previous = points[i - 1];
			var corner = points[i];
			var next = points[i + 1];
			var incoming = RouteGeometry.UnitVector(
				corner.X - previous.X,
				corner.Z - previous.Z);
			var outgoing = RouteGeometry.UnitVector(
				next.X - corner.X,
				next.Z - corner.Z);
			var cross = incoming.X * outgoing.Z - incoming.Z * outgoing.X;
			var dot = incoming.X * outgoing.X + incoming.Z * outgoing.Z;

			if (cornerRadius <= Epsilon || System.Math.Abs(cross) <= Epsilon && dot > 0.0)
			{
				AddIfDistinct(rounded, (corner.X, corner.Z));
				continue;
			}

			var incomingLength = RouteGeometry.Distance(previous, corner);
			var outgoingLength = RouteGeometry.Distance(corner, next);
			var offset = System.Math.Min(
				cornerRadius,
				System.Math.Min(incomingLength, outgoingLength) * 0.5);
			var entry = (
				X: corner.X - incoming.X * offset,
				Z: corner.Z - incoming.Z * offset);
			var exit = (
				X: corner.X + outgoing.X * offset,
				Z: corner.Z + outgoing.Z * offset);

			AddIfDistinct(rounded, entry);
			for (var step = 1; step <= cornerSubdivisions; step++)
			{
				var t = step / (double)cornerSubdivisions;
				var inverse = 1.0 - t;
				AddIfDistinct(
					rounded,
					(
						inverse * inverse * entry.X
							+ 2.0 * inverse * t * corner.X
							+ t * t * exit.X,
						inverse * inverse * entry.Z
							+ 2.0 * inverse * t * corner.Z
							+ t * t * exit.Z));
			}
		}

		AddIfDistinct(rounded, (points[^1].X, points[^1].Z));
		return rounded.ToArray();
	}

	private static IReadOnlyList<Coord> SimplifyCollinear(IReadOnlyList<Coord> points)
	{
		if (points.Count < 3)
			return points;

		var simplified = new List<Coord> { points[0] };
		for (var i = 1; i < points.Count - 1; i++)
		{
			var previous = simplified[^1];
			var current = points[i];
			var next = points[i + 1];
			var incomingX = current.X - previous.X;
			var incomingZ = current.Z - previous.Z;
			var outgoingX = next.X - current.X;
			var outgoingZ = next.Z - current.Z;
			var cross = incomingX * (long)outgoingZ - incomingZ * (long)outgoingX;
			var dot = incomingX * (long)outgoingX + incomingZ * (long)outgoingZ;
			if (cross != 0 || dot <= 0)
				simplified.Add(current);
		}

		simplified.Add(points[^1]);
		return simplified;
	}

	private static void AddIfDistinct(
		List<(double X, double Z)> points,
		(double X, double Z) point)
	{
		var previous = points[^1];
		var dx = point.X - previous.X;
		var dz = point.Z - previous.Z;
		if (dx * dx + dz * dz > Epsilon * Epsilon)
			points.Add(point);
	}

	private static double[] BuildCumulativeLengths(
		IReadOnlyList<(double X, double Z)> points)
	{
		var lengths = new double[points.Count];
		for (var i = 1; i < points.Count; i++)
		{
			var dx = points[i].X - points[i - 1].X;
			var dz = points[i].Z - points[i - 1].Z;
			lengths[i] = lengths[i - 1] + System.Math.Sqrt(dx * dx + dz * dz);
		}

		return lengths;
	}
}
