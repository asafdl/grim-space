using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Picking;

public readonly record struct MovementPickCandidate(
	Coord Coordinate,
	float DistancePx,
	float RadiusPx,
	float CameraDepth);

public readonly record struct MovementPickPreference(bool CanHit, int AlignedAxes, long AlignmentError);

public static class MovementPick
{
	private const float AssistanceBandPx = 6f;
	private const float SnapBandPx = 18f;
	private const float SnapReleaseBandPx = 24f;
	private const float DirectAimPx = 1f;
	private const float TieBandPx = 2f;
	private const float MinimumHysteresisPx = 2f;
	private const float HysteresisFraction = 0.20f;

	public static Coord? Resolve(
		IReadOnlyList<MovementPickCandidate> candidates,
		Coord? currentHovered = null,
		Func<Coord, MovementPickPreference>? preference = null)
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

		if (preference is not null)
			return SelectAssisted(eligible, currentHovered, preference);

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

	private static Coord? SelectAssisted(
		IReadOnlyList<MovementPickCandidate> eligible,
		Coord? currentHovered,
		Func<Coord, MovementPickPreference> preference)
	{
		var minimum = eligible.Min(candidate => candidate.DistancePx);
		if (minimum <= DirectAimPx)
			return SelectDeterministic(eligible.Where(candidate => candidate.DistancePx == minimum).ToArray());

		var nearby = eligible.Where(candidate =>
			candidate.DistancePx <= minimum + SnapBandPx
			|| candidate.Coordinate == currentHovered && candidate.DistancePx <= minimum + SnapReleaseBandPx)
			.Select(candidate => (Candidate: candidate, Preference: preference(candidate.Coordinate)))
			.Where(item => item.Candidate.DistancePx <= minimum + AssistanceBandPx
				|| item.Preference.CanHit || item.Preference.AlignedAxes == 2)
			.ToArray();
		if (nearby.Length == 1)
			return nearby[0].Candidate.Coordinate;

		var ranked = nearby
			.OrderByDescending(item => item.Preference.CanHit)
			.ThenByDescending(item => item.Preference.AlignedAxes)
			.ThenBy(item => item.Preference.AlignmentError)
			.ToArray();
		var best = ranked[0].Preference;
		var finalists = ranked.Where(item => item.Preference == best).Select(item => item.Candidate).ToArray();
		// Strong destinations get a wider release band; ordinary alignment stays a weak tie-break.
		return Resolve(finalists, currentHovered);
	}

	public static MovementPickPreference Alignment(Coord coordinate, Coord enemy, bool canHit)
	{
		var distances = new[]
		{
			System.Math.Abs((long)coordinate.X - enemy.X),
			System.Math.Abs((long)coordinate.Y - enemy.Y),
			System.Math.Abs((long)coordinate.Z - enemy.Z),
		};
		Array.Sort(distances);
		return new MovementPickPreference(canHit, distances.Count(distance => distance == 0),
			distances[0] + distances[1]);
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
