using GrimSpace.Math;
using GrimSpace.Math.Grid;
using GrimSpace.Math.Routes;
using GrimSpace.World.StarSystem.Landmarks;

namespace GrimSpace.World.StarSystem.Areas;

public sealed record AreaPickerArgs(
	IReadOnlyList<string> LandmarkCandidateIds,
	int MinimumPoiClearance = 0,
	double? MaximumReferenceDistance = null,
	long? DeterministicPickMix = null,
	EAreaPickerReferenceMode ReferenceMode = EAreaPickerReferenceMode.TriangulateLandmarks,
	AreaBorderReferenceConfig? BorderReferenceConfig = null);

public static class AreaPicker
{
	private const double AreaEpsilon = 0.000001;
	private const int SamplesPerSpawnAttempt = 64;
	private const int MinimumBorderLegOffset = 16;

	public static bool TryPick(StarMap map, AreaPickerArgs args, out AreaPick pick) =>
		TryPick(map, args, [DefaultSpawnSeed(args)], out pick);

	public static bool TryPick(
		StarMap map,
		AreaPickerArgs args,
		IReadOnlyList<ulong> spawnSeeds,
		out AreaPick pick)
	{
		ArgumentNullException.ThrowIfNull(map);
		ArgumentNullException.ThrowIfNull(args);
		ArgumentNullException.ThrowIfNull(spawnSeeds);
		if (spawnSeeds.Count == 0)
			throw new ArgumentException("At least one spawn seed is required.", nameof(spawnSeeds));
		ArgumentOutOfRangeException.ThrowIfNegative(args.MinimumPoiClearance);

		foreach (var landmarkId in args.LandmarkCandidateIds)
		{
			if (!MapLandmarkQueries.TryGet(map, landmarkId, out _))
				throw new ArgumentException($"Unknown landmark '{landmarkId}'.", nameof(args));
		}

		if (args.ReferenceMode == EAreaPickerReferenceMode.LandmarkWithBorderTriangle)
			return TryPickLandmarkWithBorder(map, args, spawnSeeds, out pick);

		return TryPickTriangulatedLandmarks(map, args, spawnSeeds, out pick);
	}

	private static ulong DefaultSpawnSeed(AreaPickerArgs args) =>
		args.DeterministicPickMix is long mix ? (ulong)mix : 0UL;

	private static bool TryPickLandmarkWithBorder(
		StarMap map,
		AreaPickerArgs args,
		IReadOnlyList<ulong> spawnSeeds,
		out AreaPick pick)
	{
		if (args.LandmarkCandidateIds.Count < 1)
		{
			pick = null!;
			return false;
		}

		var borderConfig = args.BorderReferenceConfig ?? new AreaBorderReferenceConfig();
		if (borderConfig.MaxDistanceFromBorderFractionOfMapDiagonal < 0.0)
		{
			throw new ArgumentOutOfRangeException(
				nameof(args),
				"Max distance from border fraction must be non-negative.");
		}

		var random = CreateRandom(args.DeterministicPickMix);
		var mapDiagonal = MapDiagonal(map);
		var maxDistanceFromBorder = borderConfig.MaxDistanceFromBorderFractionOfMapDiagonal * mapDiagonal;

		var landmarkOrder = args.LandmarkCandidateIds.ToArray();
		Shuffle(landmarkOrder, random);

		foreach (var landmarkId in landmarkOrder)
		{
			MapLandmarkQueries.TryGet(map, landmarkId, out var landmark);
			var anchor = landmark.Position;
			var borderProjection = NearestBorderProjection(map, anchor);

			if (RouteGeometry.Distance(anchor, borderProjection) > maxDistanceFromBorder)
				continue;

			if (!TryCreateBorderBase(map, borderProjection, random, out var borderA, out var borderB))
				continue;

			if (!TrySampleSpawnPoints(
				map,
				args,
				spawnSeeds,
				anchor,
				borderA,
				borderB,
				out var spawnPoints))
			{
				continue;
			}

			pick = BuildBorderPick(map, landmarkId, anchor, borderA, borderB, spawnPoints);
			return true;
		}

		pick = null!;
		return false;
	}

	private static bool TryCreateBorderBase(
		StarMap map,
		Coord projection,
		StableRandom? random,
		out Coord borderA,
		out Coord borderB)
	{
		borderA = default;
		borderB = default;
		var maxX = map.Width - 1;
		var maxZ = map.Height - 1;
		var horizontal = projection.Z == 0 || projection.Z == maxZ;
		var coordinate = horizontal ? projection.X : projection.Z;
		var maximumCoordinate = horizontal ? maxX : maxZ;
		if (coordinate <= 0 || coordinate >= maximumCoordinate)
			return false;

		var minimumOffset = MinimumBorderLegOffset;
		var negativeOffset = PickBorderOffset(coordinate, minimumOffset, random);
		var positiveOffset = PickBorderOffset(maximumCoordinate - coordinate, minimumOffset, random);
		borderA = horizontal
			? new Coord(coordinate - negativeOffset, 0, projection.Z)
			: new Coord(projection.X, 0, coordinate - negativeOffset);
		borderB = horizontal
			? new Coord(coordinate + positiveOffset, 0, projection.Z)
			: new Coord(projection.X, 0, coordinate + positiveOffset);
		return true;
	}

	private static int PickBorderOffset(int available, int minimum, StableRandom? random)
	{
		var min = System.Math.Min(minimum, available);
		return min + PickIndex(available - min + 1, random);
	}

	private static Coord NearestBorderProjection(StarMap map, Coord point)
	{
		var maxX = map.Width - 1;
		var maxZ = map.Height - 1;
		var nearest = new Coord(point.X, 0, 0);
		var distance = point.Z;
		if (maxZ - point.Z < distance)
		{
			nearest = new Coord(point.X, 0, maxZ);
			distance = maxZ - point.Z;
		}

		if (point.X < distance)
		{
			nearest = new Coord(0, 0, point.Z);
			distance = point.X;
		}

		if (maxX - point.X < distance)
			nearest = new Coord(maxX, 0, point.Z);

		return nearest;
	}

	private static AreaPick BuildBorderPick(
		StarMap map,
		string landmarkId,
		Coord anchor,
		Coord borderA,
		Coord borderB,
		Coord[] spawnPoints)
	{
		var borderAId = AreaBorderAnchor.Id(borderA);
		var borderBId = AreaBorderAnchor.Id(borderB);
		var intelAnchor = spawnPoints[0];
		var (closestId, secondClosestId, thirdClosestId) = AreaIntelProducer.OrderLandmarksByDistanceFromCenter(
			intelAnchor,
			landmarkId,
			borderAId,
			borderBId,
			id => id switch
			{
				_ when id == landmarkId => anchor,
				_ when AreaBorderAnchor.TryParseId(id, out var border) => border,
				_ => throw new InvalidOperationException($"Unknown reference id '{id}'."),
			});

		var intel = AreaIntelProducer.Produce(
			new AreaIntelContext(closestId, secondClosestId, thirdClosestId),
			[EAreaIntelTone.Brief]);

		return new AreaPick(intel, spawnPoints);
	}

	private static bool TryPickTriangulatedLandmarks(
		StarMap map,
		AreaPickerArgs args,
		IReadOnlyList<ulong> spawnSeeds,
		out AreaPick pick)
	{
		if (args.LandmarkCandidateIds.Count < 3)
		{
			pick = null!;
			return false;
		}

		var random = CreateRandom(args.DeterministicPickMix);
		var positions = args.LandmarkCandidateIds
			.Select(id =>
			{
				MapLandmarkQueries.TryGet(map, id, out var landmark);
				return (Id: id, landmark.Position);
			})
			.ToDictionary(entry => entry.Id, entry => entry.Position, StringComparer.Ordinal);

		var combinations = Combinations(args.LandmarkCandidateIds, 3).ToArray();
		Shuffle(combinations, random);

		foreach (var combination in combinations)
		{
			var a = positions[combination[0]];
			var b = positions[combination[1]];
			var c = positions[combination[2]];
			if (TriangleSignedArea(a, b, c) is var area && System.Math.Abs(area) <= AreaEpsilon)
				continue;

			if (!TrySampleSpawnPoints(map, args, spawnSeeds, a, b, c, out var spawnPoints))
				continue;

			var intelAnchor = spawnPoints[0];
			var (closestId, secondClosestId, thirdClosestId) = AreaIntelProducer.OrderLandmarksByDistanceFromCenter(
				intelAnchor,
				combination[0],
				combination[1],
				combination[2],
				id => positions[id]);
			var intel = AreaIntelProducer.Produce(
				new AreaIntelContext(closestId, secondClosestId, thirdClosestId));

			pick = new AreaPick(intel, spawnPoints);
			return true;
		}

		pick = null!;
		return false;
	}

	private static bool TrySampleSpawnPoints(
		StarMap map,
		AreaPickerArgs args,
		IReadOnlyList<ulong> spawnSeeds,
		Coord cornerA,
		Coord cornerB,
		Coord cornerC,
		out Coord[] spawnPoints)
	{
		spawnPoints = new Coord[spawnSeeds.Count];
		for (var index = 0; index < spawnSeeds.Count; index++)
		{
			if (!TrySampleSpawnInTriangle(
				map,
				args,
				cornerA,
				cornerB,
				cornerC,
				spawnSeeds[index],
				out var spawn))
			{
				spawnPoints = [];
				return false;
			}

			spawnPoints[index] = spawn;
		}

		return true;
	}

	private static bool TrySampleSpawnInTriangle(
		StarMap map,
		AreaPickerArgs args,
		Coord cornerA,
		Coord cornerB,
		Coord cornerC,
		ulong seed,
		out Coord spawn)
	{
		spawn = default;
		var random = new StableRandom(seed);
		for (var attempt = 0; attempt < SamplesPerSpawnAttempt; attempt++)
		{
			var candidate = SampleUniformTrianglePoint(cornerA, cornerB, cornerC, random);
			if (!map.IsInBounds(candidate) || !map.PathfindingTerrain.IsTraversable(candidate))
				continue;

			if (!MeetsPoiClearance(map, candidate, args.MinimumPoiClearance))
				continue;

			if (!MeetsMaximumReferenceDistance(
				candidate,
				cornerA,
				cornerB,
				cornerC,
				args.MaximumReferenceDistance))
			{
				continue;
			}

			spawn = candidate;
			return true;
		}

		return false;
	}

	private static Coord SampleUniformTrianglePoint(
		Coord a,
		Coord b,
		Coord c,
		StableRandom random)
	{
		var r1 = random.NextDouble();
		var r2 = random.NextDouble();
		if (r1 + r2 > 1.0)
		{
			r1 = 1.0 - r1;
			r2 = 1.0 - r2;
		}

		var r3 = 1.0 - r1 - r2;
		var x = r1 * a.X + r2 * b.X + r3 * c.X;
		var z = r1 * a.Z + r2 * b.Z + r3 * c.Z;
		return new Coord((int)System.Math.Round(x), 0, (int)System.Math.Round(z));
	}

	private static bool MeetsMaximumReferenceDistance(
		Coord coord,
		Coord cornerA,
		Coord cornerB,
		Coord cornerC,
		double? maximumReferenceDistance)
	{
		if (maximumReferenceDistance is not double maxReference)
			return true;

		var maxDistance = System.Math.Max(
			RouteGeometry.Distance(coord, cornerA),
			System.Math.Max(
				RouteGeometry.Distance(coord, cornerB),
				RouteGeometry.Distance(coord, cornerC)));
		return maxDistance <= maxReference;
	}

	private static bool MeetsPoiClearance(StarMap map, Coord coord, int minimumPoiClearance)
	{
		foreach (var poi in map.PointsOfInterest)
		{
			var clearance = poi.RouteExclusionRadius + minimumPoiClearance;
			if (RouteGeometry.Distance(coord, poi.PlacedCenter) < clearance)
				return false;
		}

		return true;
	}

	private static StableRandom? CreateRandom(long? mix) =>
		mix is long seed ? new StableRandom((ulong)seed) : null;

	private static void Shuffle(string[] items, StableRandom? random)
	{
		for (var i = items.Length - 1; i > 0; i--)
		{
			var j = PickIndex(i + 1, random);
			(items[i], items[j]) = (items[j], items[i]);
		}
	}

	private static void Shuffle(string[][] items, StableRandom? random)
	{
		for (var i = items.Length - 1; i > 0; i--)
		{
			var j = PickIndex(i + 1, random);
			(items[i], items[j]) = (items[j], items[i]);
		}
	}

	private static double MapDiagonal(StarMap map) =>
		RouteGeometry.Distance(new Coord(0, 0, 0), new Coord(map.Width - 1, 0, map.Height - 1));

	private static double TriangleSignedArea(Coord p, Coord q, Coord r) =>
		(q.X - p.X) * (double)(r.Z - p.Z) - (r.X - p.X) * (double)(q.Z - p.Z);

	private static int PickIndex(int count, StableRandom? random) =>
		count <= 0
			? 0
			: random is null
				? 0
				: (int)(random.Value.NextDouble() * count);

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

}
