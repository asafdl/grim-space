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
