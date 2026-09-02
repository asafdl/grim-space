using GrimSpace.Math.Grid;
using GrimSpace.Math.Routes;
using GrimSpace.World.StarSystem.Poi;

namespace GrimSpace.World.StarSystem.Areas;

public static class AreaPicker
{
	private const int GeometryClearance = 8;
	private const int SamplesPerAxis = 16;
	private const double MinAxisFraction = 0.20;
	private const double MaxAxisFraction = 0.80;

	public static AreaPick Pick(
		StarMap map,
		IReadOnlyCollection<IReadOnlyCollection<string>> landmarkGroups,
		IReadOnlyCollection<EAreaDistance> allowedDistances,
		int landmarksToPick,
		AreaDistanceConfig? distanceConfig = null,
		AreaRadiusConfig? radiusConfig = null)
	{
		ArgumentNullException.ThrowIfNull(map);
		ArgumentNullException.ThrowIfNull(landmarkGroups);
		ArgumentNullException.ThrowIfNull(allowedDistances);
		if (landmarksToPick < 1)
			throw new ArgumentException("landmarksToPick must be at least 1.", nameof(landmarksToPick));
		if (landmarksToPick != 2)
			throw new ArgumentException("Only landmarksToPick == 2 is supported.", nameof(landmarksToPick));
		if (landmarkGroups.Count == 0)
			throw new ArgumentException("landmarkGroups must not be empty.", nameof(landmarkGroups));
		if (allowedDistances.Count == 0)
			throw new ArgumentException("allowedDistances must not be empty.", nameof(allowedDistances));

		distanceConfig ??= new AreaDistanceConfig();
		radiusConfig ??= new AreaRadiusConfig();

		var poiById = map.PointsOfInterest.ToDictionary(poi => poi.Id, StringComparer.Ordinal);
		ValidateLandmarkGroups(landmarkGroups, landmarksToPick, poiById);

		var group = PickRandom(landmarkGroups.ToArray());
		var combination = PickRandom(Combinations(group, landmarksToPick).ToArray());
		var distance = PickRandom(allowedDistances.ToArray());

		var landmarkAId = combination[0];
		var landmarkBId = combination[1];
		var centerA = poiById[landmarkAId].PlacedCenter;
		var centerB = poiById[landmarkBId].PlacedCenter;
		var span = RouteGeometry.Distance(centerA, centerB);
		if (span <= 0.0)
		{
			throw new InvalidOperationException(
				$"Sampled landmarks '{landmarkAId}' and '{landmarkBId}' share the same position.");
		}

		var radius = AreaRadiusPicker.Pick(span, radiusConfig);
		var axis = new[] { centerA, centerB };
		var candidates = new List<Candidate>();
		CollectCandidates(
			map,
			candidates,
			axis,
			span,
			span,
			radius,
			landmarkAId,
			landmarkBId,
			distance,
			distanceConfig);

		if (candidates.Count == 0)
		{
			throw new InvalidOperationException(
				$"No valid area for landmarks '{landmarkAId}' and '{landmarkBId}' at distance {distance}.");
		}

		var chosen = candidates[Random.Shared.Next(candidates.Count)];
		var displayA = poiById[chosen.LandmarkAId].DisplayName;
		var displayB = poiById[chosen.LandmarkBId].DisplayName;
		var intel = AreaIntelProducer.Produce(new AreaIntelContext(displayA, displayB, chosen.Distance));

		return new AreaPick(
			chosen.Center,
			chosen.Radius,
			intel,
			new AreaRelation.BetweenLandmarks(chosen.LandmarkAId, chosen.LandmarkBId, chosen.Distance));
	}

	private static void ValidateLandmarkGroups(
		IReadOnlyCollection<IReadOnlyCollection<string>> landmarkGroups,
		int landmarksToPick,
		IReadOnlyDictionary<string, PointOfInterest> poiById)
	{
		foreach (var group in landmarkGroups)
		{
			if (group.Count == 0)
				throw new ArgumentException("Landmark group must not be empty.", nameof(landmarkGroups));

			if (group.Count < landmarksToPick)
			{
				throw new ArgumentException(
					$"Landmark group has {group.Count} landmark(s) but {landmarksToPick} required.",
					nameof(landmarkGroups));
			}

			foreach (var landmarkId in group)
			{
				if (!poiById.ContainsKey(landmarkId))
					throw new ArgumentException($"Unknown landmark '{landmarkId}'.", nameof(landmarkGroups));
			}
		}
	}

	private static T PickRandom<T>(IReadOnlyList<T> items) => items[Random.Shared.Next(items.Count)];

	private static void CollectCandidates(
		StarMap map,
		List<Candidate> candidates,
		IReadOnlyList<Coord> axis,
		double axisLength,
		double span,
		int radius,
		string landmarkAId,
		string landmarkBId,
		EAreaDistance distance,
		AreaDistanceConfig distanceConfig)
	{
		for (var sample = 0; sample < SamplesPerAxis; sample++)
		{
			var fraction = SamplesPerAxis == 1
				? (MinAxisFraction + MaxAxisFraction) * 0.5
				: MinAxisFraction + (MaxAxisFraction - MinAxisFraction) * sample / (SamplesPerAxis - 1);
			var arcLength = axisLength * fraction;
			var (sampleX, sampleZ, tangentX, tangentZ) = RouteGeometry.SampleAtArcLength(axis, arcLength);
			var perpendicularX = -tangentZ;
			var perpendicularZ = tangentX;

			var lateralDistance = SampleLateralDistance(span, radius, distance, distanceConfig);
			if (lateralDistance is null)
				continue;

			var side = Random.Shared.Next(2) == 0 ? 1.0 : -1.0;
			var center = new Coord(
				(int)System.Math.Round(sampleX + perpendicularX * lateralDistance.Value * side),
				0,
				(int)System.Math.Round(sampleZ + perpendicularZ * lateralDistance.Value * side));

			if (!IsValidCandidate(map, center, radius, axis, span, distance, distanceConfig))
				continue;

			candidates.Add(new Candidate(center, radius, landmarkAId, landmarkBId, distance));
		}
	}

	private static double? SampleLateralDistance(
		double span,
		int radius,
		EAreaDistance distance,
		AreaDistanceConfig config)
	{
		var (min, max) = LateralSampleRange(span, radius, distance, config);
		if (min > max)
			return null;

		return min + Random.Shared.NextDouble() * (max - min);
	}

	private static (double Min, double Max) LateralSampleRange(
		double span,
		int radius,
		EAreaDistance distance,
		AreaDistanceConfig config) =>
		distance switch
		{
			EAreaDistance.Low => (0.0, span * config.LowFraction - radius),
			EAreaDistance.Med => (span * config.MedMinFraction + radius, span * config.MedMaxFraction - radius),
			EAreaDistance.High => (span * config.HighMinFraction + radius, span),
			_ => throw new ArgumentOutOfRangeException(nameof(distance), distance, null),
		};

	private static bool IsValidCandidate(
		StarMap map,
		Coord center,
		int radius,
		IReadOnlyList<Coord> axis,
		double span,
		EAreaDistance distance,
		AreaDistanceConfig config)
	{
		if (!GridBounds.IsCircleWhollyInRectangle(center, radius, map.Width, map.Height))
			return false;

		foreach (var poi in map.PointsOfInterest)
		{
			var clearance = RouteGeometry.Distance(center, poi.PlacedCenter);
			if (clearance < poi.Radius + GeometryClearance + radius)
				return false;
		}

		var axisDistance = RouteGeometry.PointToPolylineDistance(center, axis);
		return distance switch
		{
			EAreaDistance.Low => axisDistance + radius <= span * config.LowFraction,
			EAreaDistance.Med =>
				axisDistance - radius >= span * config.MedMinFraction
				&& axisDistance + radius <= span * config.MedMaxFraction,
			EAreaDistance.High => axisDistance - radius >= span * config.HighMinFraction,
			_ => false,
		};
	}

	private static IEnumerable<string[]> Combinations(IReadOnlyCollection<string> group, int count)
	{
		var items = group.ToArray();
		var current = new string[count];
		foreach (var combination in Combine(items, 0, 0, current))
			yield return combination;
	}

	private static IEnumerable<string[]> Combine(
		string[] items,
		int start,
		int depth,
		string[] current)
	{
		if (depth == current.Length)
		{
			yield return (string[])current.Clone();
			yield break;
		}

		for (var i = start; i <= items.Length - (current.Length - depth); i++)
		{
			current[depth] = items[i];
			foreach (var combination in Combine(items, i + 1, depth + 1, current))
				yield return combination;
		}
	}

	private sealed record Candidate(
		Coord Center,
		int Radius,
		string LandmarkAId,
		string LandmarkBId,
		EAreaDistance Distance);
}
