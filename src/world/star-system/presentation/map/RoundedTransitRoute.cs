using System.Runtime.CompilerServices;
using GrimSpace.Math.Grid;
using GrimSpace.Math.Routes;
using GrimSpace.World.StarSystem.Pathfinding;

namespace GrimSpace.World.StarSystem.Presentation.Map;

internal sealed class RoundedTransitRoute
{
	private const double MicroCorrectionTolerance = 3.0;
	private const double CornerRadius = 12.0;
	private const int CornerSubdivisions = 12;

	private static readonly ConditionalWeakTable<TransitPath, RoundedTransitRoute> Cache = new();

	private readonly TransitPath _source;
	private readonly RoundedPolyline _route;

	private RoundedTransitRoute(TransitPath source)
	{
		_source = source;
		_route = new RoundedPolyline(
			SimplifyMicroCorrections(FlattenPoints(source)),
			CornerRadius,
			CornerSubdivisions);
	}

	public IReadOnlyList<(double X, double Z)> Points => _route.Points;

	public static RoundedTransitRoute For(TransitPath source) =>
		Cache.GetValue(source, static path => new RoundedTransitRoute(path));

	public RouteSample SampleAtElapsed(double elapsedTicks, double speedPerTick)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(speedPerTick);
		if (_source.TotalLength <= 0.0)
			return _route.SampleNormalized(1.0);

		var remainingTicks = System.Math.Max(0.0, elapsedTicks);
		var traveledDistance = 0.0;
		foreach (var leg in _source.Legs)
		{
			var speed = speedPerTick * leg.SpeedMultiplier;
			var legDuration = leg.Length / speed;
			if (remainingTicks < legDuration)
			{
				traveledDistance += remainingTicks * speed;
				return _route.SampleNormalized(traveledDistance / _source.TotalLength);
			}

			remainingTicks -= legDuration;
			traveledDistance += leg.Length;
		}

		return _route.SampleNormalized(1.0);
	}

	public IReadOnlyList<(double X, double Z)> RemainingPoints(RouteSample sample)
	{
		var remaining = new List<(double X, double Z)>
		{
			(sample.X, sample.Z),
		};
		for (var i = sample.NextPointIndex; i < _route.Points.Count; i++)
			remaining.Add(_route.Points[i]);
		return remaining;
	}

	private static IReadOnlyList<Coord> FlattenPoints(TransitPath path)
	{
		var points = new List<Coord>();
		foreach (var leg in path.Legs)
		{
			foreach (var point in leg.Points)
			{
				if (points.Count == 0 || points[^1] != point)
					points.Add(point);
			}
		}

		if (points.Count == 0)
			throw new ArgumentException("Transit path must contain at least one point.", nameof(path));
		return points;
	}

	private static IReadOnlyList<Coord> SimplifyMicroCorrections(IReadOnlyList<Coord> points)
	{
		if (points.Count < 3)
			return points;

		var keep = new bool[points.Count];
		keep[0] = true;
		keep[^1] = true;
		KeepMeaningfulDetours(points, 0, points.Count - 1, keep);

		var simplified = new List<Coord>();
		for (var i = 0; i < points.Count; i++)
		{
			if (keep[i])
				simplified.Add(points[i]);
		}

		return simplified;
	}

	private static void KeepMeaningfulDetours(
		IReadOnlyList<Coord> points,
		int startIndex,
		int endIndex,
		bool[] keep)
	{
		var furthestIndex = -1;
		var furthestDistance = 0.0;
		for (var i = startIndex + 1; i < endIndex; i++)
		{
			var distance = RouteGeometry.PointToSegmentDistance(
				points[i],
				points[startIndex],
				points[endIndex]);
			if (distance <= furthestDistance)
				continue;

			furthestDistance = distance;
			furthestIndex = i;
		}

		if (furthestIndex < 0 || furthestDistance <= MicroCorrectionTolerance)
			return;

		keep[furthestIndex] = true;
		KeepMeaningfulDetours(points, startIndex, furthestIndex, keep);
		KeepMeaningfulDetours(points, furthestIndex, endIndex, keep);
	}
}
