using GrimSpace.Math;
using GrimSpace.Math.Grid;
using GrimSpace.Math.Routes;
using GrimSpace.World.StarSystem.Landmarks;

namespace GrimSpace.World.StarSystem.Areas;

public sealed record AreaPickerArgs(
	IReadOnlyList<string> LandmarkCandidateIds,
	int MinimumPoiClearance = 0,
	double? MaximumReferenceDistance = null,
	AreaRadiusConfig? RadiusConfig = null,
	long? DeterministicPickMix = null);

public static class AreaPicker
{
	public static bool TryPick(StarMap map, AreaPickerArgs args, out AreaPick pick)
	{
		ArgumentNullException.ThrowIfNull(map);
		ArgumentNullException.ThrowIfNull(args);
		ArgumentOutOfRangeException.ThrowIfNegative(args.MinimumPoiClearance);

		if (args.LandmarkCandidateIds.Count < 3)
		{
			pick = null!;
			return false;
		}

		foreach (var landmarkId in args.LandmarkCandidateIds)
		{
			if (!MapLandmarkQueries.TryGet(map, landmarkId, out _))
				throw new ArgumentException($"Unknown landmark '{landmarkId}'.", nameof(args));
		}

		var radiusConfig = args.RadiusConfig ?? new AreaRadiusConfig();
		var radius = ResolveSearchRadius(map, args.LandmarkCandidateIds, radiusConfig);
		if (radius <= 0)
		{
			pick = null!;
			return false;
		}

		StableRandom? random = args.DeterministicPickMix is long mix
			? new StableRandom((ulong)mix)
			: null;

		var positions = args.LandmarkCandidateIds
			.Select(id =>
			{
				MapLandmarkQueries.TryGet(map, id, out var landmark);
				return (Id: id, landmark.Position);
			})
			.ToArray();

		var combinations = Combinations(args.LandmarkCandidateIds, 3).ToArray();
		var bestScore = double.PositiveInfinity;
		var best = new List<Solution>();

		foreach (var center in SampleCandidateCenters(map, radius))
		{
			if (!MeetsPoiClearance(map, center, radius, args.MinimumPoiClearance))
				continue;

			foreach (var combination in combinations)
			{
				var a = positions.First(entry => entry.Id == combination[0]);
				var b = positions.First(entry => entry.Id == combination[1]);
				var c = positions.First(entry => entry.Id == combination[2]);

				if (!CircleWhollyInsideTriangle(center, radius, a.Position, b.Position, c.Position))
					continue;

				var maxReferenceDistance = System.Math.Max(
					RouteGeometry.Distance(center, a.Position),
					System.Math.Max(
						RouteGeometry.Distance(center, b.Position),
						RouteGeometry.Distance(center, c.Position)));

				if (args.MaximumReferenceDistance is double maxReference
					&& maxReferenceDistance > maxReference)
				{
					continue;
				}

				if (maxReferenceDistance < bestScore)
				{
					bestScore = maxReferenceDistance;
					best.Clear();
				}

				if (System.Math.Abs(maxReferenceDistance - bestScore) <= 0.000001)
				{
					best.Add(new Solution(
						center,
						radius,
						combination[0],
						combination[1],
						combination[2]));
				}
			}
		}

		if (best.Count == 0)
		{
			pick = null!;
			return false;
		}

		var chosen = best[PickIndex(best.Count, random)];
		var (closestId, secondClosestId, thirdClosestId) = AreaIntelProducer.OrderLandmarksByDistanceFromCenter(
			chosen.Center,
			chosen.LandmarkAId,
			chosen.LandmarkBId,
			chosen.LandmarkCId,
			id => positions.First(entry => entry.Id == id).Position);
		var intel = AreaIntelProducer.Produce(
			new AreaIntelContext(closestId, secondClosestId, thirdClosestId));

		pick = new AreaPick(
			chosen.Center,
			chosen.Radius,
			intel,
			new AreaRelation.TriangulatedLandmarks(
				chosen.LandmarkAId,
				chosen.LandmarkBId,
				chosen.LandmarkCId));
		return true;
	}

	private static int ResolveSearchRadius(
		StarMap map,
		IReadOnlyList<string> landmarkCandidateIds,
		AreaRadiusConfig radiusConfig)
	{
		var span = MaxPairwiseSpan(map, landmarkCandidateIds);
		return span > 0.0
			? AreaRadiusPicker.Pick(span, radiusConfig)
			: radiusConfig.MinRadius;
	}

	private static double MaxPairwiseSpan(StarMap map, IReadOnlyList<string> landmarkIds)
	{
		var span = 0.0;
		for (var i = 0; i < landmarkIds.Count; i++)
		{
			MapLandmarkQueries.TryGet(map, landmarkIds[i], out var left);
			for (var j = i + 1; j < landmarkIds.Count; j++)
			{
				MapLandmarkQueries.TryGet(map, landmarkIds[j], out var right);
				span = System.Math.Max(span, RouteGeometry.Distance(left.Position, right.Position));
			}
		}

		return span;
	}

	private static IEnumerable<Coord> SampleCandidateCenters(StarMap map, int radius)
	{
		var step = System.Math.Max(radius * 2, 16);
		for (var z = radius; z < map.Height - radius; z += step)
		{
			for (var x = radius; x < map.Width - radius; x += step)
			{
				var center = new Coord(x, 0, z);
				if (map.PathfindingTerrain.IsCircleTraversable(center, radius))
					yield return center;
			}
		}
	}

	private static bool MeetsPoiClearance(StarMap map, Coord center, int radius, int minimumPoiClearance)
	{
		foreach (var poi in map.PointsOfInterest)
		{
			var clearance = poi.RouteExclusionRadius + minimumPoiClearance + radius;
			if (RouteGeometry.Distance(center, poi.PlacedCenter) < clearance)
				return false;
		}

		return true;
	}

	private static bool CircleWhollyInsideTriangle(
		Coord center,
		int radius,
		Coord a,
		Coord b,
		Coord c)
	{
		if (!IsPointInsideTriangle(center, a, b, c))
			return false;

		var minEdgeDistance = RouteGeometry.PointToSegmentDistance(center, a, b);
		minEdgeDistance = System.Math.Min(minEdgeDistance, RouteGeometry.PointToSegmentDistance(center, b, c));
		minEdgeDistance = System.Math.Min(minEdgeDistance, RouteGeometry.PointToSegmentDistance(center, c, a));
		return minEdgeDistance >= radius;
	}

	private static bool IsPointInsideTriangle(Coord point, Coord a, Coord b, Coord c)
	{
		var area = TriangleSignedArea(a, b, c);
		if (System.Math.Abs(area) <= 0.000001)
			return false;

		var sign = area > 0.0 ? 1.0 : -1.0;
		return TriangleSignedArea(point, a, b) * sign >= 0.0
			&& TriangleSignedArea(point, b, c) * sign >= 0.0
			&& TriangleSignedArea(point, c, a) * sign >= 0.0;
	}

	private static double TriangleSignedArea(Coord p, Coord q, Coord r) =>
		(q.X - p.X) * (double)(r.Z - p.Z) - (r.X - p.X) * (double)(q.Z - p.Z);

	private static int PickIndex(int count, StableRandom? random) =>
		random is null ? Random.Shared.Next(count) : (int)(random.Value.NextDouble() * count);

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

	private sealed record Solution(
		Coord Center,
		int Radius,
		string LandmarkAId,
		string LandmarkBId,
		string LandmarkCId);
}
