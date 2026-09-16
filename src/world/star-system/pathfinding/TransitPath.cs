using System.Collections.Immutable;
using GrimSpace.Math.Grid;
using GrimSpace.Math.Routes;

namespace GrimSpace.World.StarSystem.Pathfinding;

public sealed record TransitLeg(
	ImmutableArray<Coord> Points,
	double SpeedMultiplier,
	double Length);

public sealed record TransitPath(ImmutableArray<TransitLeg> Legs)
{
	public double TotalLength { get; } = Legs.Sum(leg => leg.Length);

	private RouteSegment[] Segments { get; } = CreateSegments(Legs);

	public double TicksRequired(double speedPerTick) =>
		PiecewiseRouteSampler.TimeRequired(Segments, speedPerTick);

	public int DurationTicks(double speedPerTick) =>
		(int)System.Math.Ceiling(TicksRequired(speedPerTick));

	public PiecewiseRouteSample SampleContinuousAtElapsed(double elapsedTicks, double speedPerTick) =>
		PiecewiseRouteSampler.SampleContinuousAtElapsed(Segments, elapsedTicks, speedPerTick);

	public (Coord Position, Coord Tangent) SampleAtElapsed(double elapsedTicks, double speedPerTick) =>
		PiecewiseRouteSampler.SampleAtElapsed(Segments, elapsedTicks, speedPerTick);

	public IReadOnlyList<(double X, double Z)> RemainingPoints(PiecewiseRouteSample sample)
	{
		var result = new List<(double, double)> { (sample.Route.X, sample.Route.Z) };

		var legIndex = sample.SegmentIndex;
		var leg = Legs[legIndex];
		var nextPointIndex = sample.Route.NextPointIndex;

		if (nextPointIndex < leg.Points.Length)
		{
			var endPoint = leg.Points[nextPointIndex];
			result.Add((endPoint.X, endPoint.Z));
		}

		for (var i = nextPointIndex + 1; i < leg.Points.Length; i++)
		{
			var point = leg.Points[i];
			result.Add((point.X, point.Z));
		}

		for (var subsequentLegIndex = legIndex + 1; subsequentLegIndex < Legs.Length; subsequentLegIndex++)
		{
			foreach (var point in Legs[subsequentLegIndex].Points)
				result.Add((point.X, point.Z));
		}

		return result;
	}

	private static RouteSegment[] CreateSegments(ImmutableArray<TransitLeg> legs)
	{
		var segments = new RouteSegment[legs.Length];
		for (var i = 0; i < legs.Length; i++)
		{
			var leg = legs[i];
			segments[i] = new RouteSegment(leg.Points, leg.Length, leg.SpeedMultiplier);
		}

		return segments;
	}

	public static TransitPath FromPoints(
		IReadOnlyList<Coord> points,
		IReadOnlyList<double> speedMultipliers)
	{
		if (points.Count == 0)
			throw new ArgumentException("Path must contain at least one point.", nameof(points));

		if (speedMultipliers.Count != points.Count)
		{
			throw new ArgumentException(
				"Speed multipliers must match point count.",
				nameof(speedMultipliers));
		}

		var legs = new List<TransitLeg>();
		var legStart = 0;
		for (var i = 1; i < points.Count; i++)
		{
			if (speedMultipliers[i] == speedMultipliers[legStart])
				continue;

			legs.Add(CreateLeg(points, speedMultipliers, legStart, i));
			legStart = i;
		}

		legs.Add(CreateLeg(points, speedMultipliers, legStart, points.Count - 1));

		return new TransitPath(legs.ToImmutableArray());
	}

	private static TransitLeg CreateLeg(
		IReadOnlyList<Coord> points,
		IReadOnlyList<double> speedMultipliers,
		int start,
		int endInclusive)
	{
		var legPoints = points.Skip(start).Take(endInclusive - start + 1).ToArray();
		return new TransitLeg(
			legPoints.ToImmutableArray(),
			speedMultipliers[start],
			PolylineSampler.Length(legPoints));
	}
}
