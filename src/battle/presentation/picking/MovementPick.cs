using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Picking;

public readonly record struct MovementPickCandidate(
	Coord Coordinate,
	float DistancePx,
	float RadiusPx,
	float CameraDepth);

public static class MovementPick
{
	private const float TieBandPx = 2f;
	private const float MinimumHysteresisPx = 2f;
	private const float HysteresisFraction = 0.20f;

	public static Coord? Resolve(
		IReadOnlyList<MovementPickCandidate> candidates,
		Coord? currentHovered = null)
	{
		if (candidates.Count == 0)
			return null;

		var eligible = new List<MovementPickCandidate>(candidates.Count);
		foreach (var candidate in candidates)
		{
			if (!IsEligible(candidate))
				continue;

			eligible.Add(candidate);
		}

		if (eligible.Count == 0)
			return null;

		var challenger = SelectDeterministic(eligible);
		if (challenger is null)
			return null;

		if (currentHovered is not Coord retained)
			return challenger;

		MovementPickCandidate? current = null;
		foreach (var candidate in eligible)
		{
			if (candidate.Coordinate != retained)
				continue;

			current = candidate;
			break;
		}

		if (current is not MovementPickCandidate currentCandidate)
			return challenger;

		var challengerCandidate = FindCandidate(eligible, challenger.Value);
		var requiredImprovementPx = System.Math.Max(
			MinimumHysteresisPx,
			HysteresisFraction * currentCandidate.DistancePx);
		if (currentCandidate.DistancePx - challengerCandidate.DistancePx > requiredImprovementPx)
			return challenger;

		return retained;
	}

	internal static Coord? SelectDeterministic(IReadOnlyList<MovementPickCandidate> eligible)
	{
		if (eligible.Count == 0)
			return null;

		var minDistance = float.PositiveInfinity;
		foreach (var candidate in eligible)
			minDistance = System.Math.Min(minDistance, candidate.DistancePx);

		var tieThreshold = minDistance + TieBandPx;
		Coord? best = null;
		var bestDepth = float.PositiveInfinity;
		var bestX = int.MaxValue;
		var bestY = int.MaxValue;
		var bestZ = int.MaxValue;

		foreach (var candidate in eligible)
		{
			if (candidate.DistancePx > tieThreshold)
				continue;

			var coord = candidate.Coordinate;
			if (best is not Coord
				|| candidate.CameraDepth < bestDepth
				|| candidate.CameraDepth == bestDepth && coord.X < bestX
				|| candidate.CameraDepth == bestDepth && coord.X == bestX && coord.Y < bestY
				|| candidate.CameraDepth == bestDepth && coord.X == bestX && coord.Y == bestY && coord.Z < bestZ)
			{
				best = coord;
				bestDepth = candidate.CameraDepth;
				bestX = coord.X;
				bestY = coord.Y;
				bestZ = coord.Z;
			}
		}

		return best;
	}

	internal static bool IsEligible(MovementPickCandidate candidate) =>
		candidate.RadiusPx > 0f
		&& candidate.CameraDepth > 0f
		&& float.IsFinite(candidate.DistancePx)
		&& float.IsFinite(candidate.RadiusPx)
		&& float.IsFinite(candidate.CameraDepth)
		&& candidate.DistancePx <= candidate.RadiusPx;

	private static MovementPickCandidate FindCandidate(
		IReadOnlyList<MovementPickCandidate> eligible,
		Coord coordinate)
	{
		foreach (var candidate in eligible)
		{
			if (candidate.Coordinate == coordinate)
				return candidate;
		}

		throw new InvalidOperationException($"Candidate {coordinate} is not eligible.");
	}
}
