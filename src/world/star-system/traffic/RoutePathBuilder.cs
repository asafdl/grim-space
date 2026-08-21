using System.Collections.ObjectModel;
using GrimSpace.Math.Grid;

namespace GrimSpace.World.StarSystem.Traffic;

public readonly record struct RoutePair(string OriginPoiId, string DestinationPoiId);

public sealed record CircularExclusion(Coord Center, int Radius);

public sealed record RouteTopology(
	IReadOnlyDictionary<string, LaneSegment> SegmentsById,
	IReadOnlyDictionary<string, RouteTemplate> RoutesById,
	IReadOnlyDictionary<RoutePair, IReadOnlyList<string>> RouteIdsByPair);

public static class RoutePathBuilder
{
	private const double SampleSpacing = 16.0;
	private const double ReservationBlockLength = 96.0;
	private const double DepartureThroatLength = 48.0;
	private const double DirectionalBandOffset = 64.0;
	private const double VariantBandSpacing = 20.0;
	private const double BaseWaveAmplitude = 9.0;
	private const double MinimumLaneClearance = 10.0;
	private const double ExclusionClearance = 6.0;
	private const double DockMergeRadius = 64.0;
	private const int MaxGenerationAttempts = 64;

	public static RouteTopology Build(
		int seed,
		int width,
		int height,
		IReadOnlyCollection<Dock> docks,
		int routesPerDirectedPair) =>
		Build(seed, width, height, docks, AllDirectedPairs(docks), [], routesPerDirectedPair);

	public static RouteTopology Build(
		int seed,
		int width,
		int height,
		IReadOnlyCollection<Dock> docks,
		IReadOnlyCollection<CircularExclusion> exclusions,
		int routesPerDirectedPair) =>
		Build(seed, width, height, docks, AllDirectedPairs(docks), exclusions, routesPerDirectedPair);

	public static RouteTopology Build(
		int seed,
		int width,
		int height,
		IReadOnlyCollection<Dock> docks,
		IReadOnlyCollection<RoutePair> pairs,
		int routesPerDirectedPair) =>
		Build(seed, width, height, docks, pairs, [], routesPerDirectedPair);

	public static RouteTopology Build(
		int seed,
		int width,
		int height,
		IReadOnlyCollection<Dock> docks,
		IReadOnlyCollection<RoutePair> pairs,
		IReadOnlyCollection<CircularExclusion> exclusions,
		int routesPerDirectedPair)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(width, 2);
		ArgumentOutOfRangeException.ThrowIfLessThan(height, 2);
		ArgumentOutOfRangeException.ThrowIfLessThan(routesPerDirectedPair, 1);
		ArgumentNullException.ThrowIfNull(pairs);

		var orderedDocks = docks
			.OrderBy(dock => dock.PoiId, StringComparer.Ordinal)
			.ThenBy(dock => dock.Id, StringComparer.Ordinal)
			.ToArray();

		ValidateDocks(orderedDocks, width, height);

		var docksByPoiId = orderedDocks.ToDictionary(dock => dock.PoiId, StringComparer.Ordinal);
		var orderedPairs = pairs
			.OrderBy(pair => pair.OriginPoiId, StringComparer.Ordinal)
			.ThenBy(pair => pair.DestinationPoiId, StringComparer.Ordinal)
			.ToArray();

		foreach (var pair in orderedPairs)
		{
			if (!docksByPoiId.ContainsKey(pair.OriginPoiId))
				throw new ArgumentException($"Unknown origin POI '{pair.OriginPoiId}'.", nameof(pairs));
			if (!docksByPoiId.ContainsKey(pair.DestinationPoiId))
				throw new ArgumentException($"Unknown destination POI '{pair.DestinationPoiId}'.", nameof(pairs));
			if (pair.OriginPoiId == pair.DestinationPoiId)
				throw new ArgumentException($"Route pair cannot share origin and destination: {pair}.", nameof(pairs));
		}

		var segments = new Dictionary<string, LaneSegment>(StringComparer.Ordinal);
		var routes = new Dictionary<string, RouteTemplate>(StringComparer.Ordinal);
		var routeIdsByPair = new Dictionary<RoutePair, List<string>>();
		var acceptedCruisePaths = new List<AcceptedPath>();

		// Every route into a destination shares its final queue -> arrival throat.
		// That convergence is therefore represented by one reservable resource.
		var arrivalThroatIds = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (var destinationPoiId in orderedPairs.Select(pair => pair.DestinationPoiId).Distinct(StringComparer.Ordinal))
		{
			var destination = docksByPoiId[destinationPoiId];
			var id = SegmentId(destination.PoiId, "arrival-throat");
			AddSegment(segments, id, [destination.QueueHold, destination.ArrivalBerth]);
			arrivalThroatIds[destination.PoiId] = id;
		}

		foreach (var pair in orderedPairs)
		{
			var origin = docksByPoiId[pair.OriginPoiId];
			var destination = docksByPoiId[pair.DestinationPoiId];
			var pairRouteIds = new List<string>(routesPerDirectedPair);
			routeIdsByPair.Add(pair, pairRouteIds);

			// Variants for this directed pair share a deterministic departure throat.
			// Routes to another destination use another throat; CP3 must retain the
			// departure berth until the ship clears that dock convergence zone.
			var departureThroatEnd = BuildDepartureThroatEnd(
				origin,
				destination,
				width,
				height,
				exclusions);
			var departureThroatId = SegmentId(
				destination.PoiId,
				$"from-{origin.PoiId}-departure-throat");
			AddSegment(
				segments,
				departureThroatId,
				[origin.DepartureBerth, departureThroatEnd]);

			for (var variant = 0; variant < routesPerDirectedPair; variant++)
			{
				var cruise = GenerateCruisePath(
					seed,
					width,
					height,
					origin,
					destination,
					departureThroatEnd,
					variant,
					routesPerDirectedPair,
					exclusions,
					orderedDocks,
					acceptedCruisePaths);

				acceptedCruisePaths.Add(new AcceptedPath(pair, cruise));

				var routeId = RouteId(origin.PoiId, destination.PoiId, variant);
				var segmentIds = new List<string> { departureThroatId };
				var blocks = SplitIntoReservationBlocks(cruise, ReservationBlockLength);

				for (var blockIndex = 0; blockIndex < blocks.Count; blockIndex++)
				{
					var blockId = SegmentId(
						destination.PoiId,
						$"from-{origin.PoiId}-v{variant}-b{blockIndex}");
					AddSegment(segments, blockId, blocks[blockIndex]);
					segmentIds.Add(blockId);
				}

				segmentIds.Add(arrivalThroatIds[destination.PoiId]);
				routes.Add(
					routeId,
					new RouteTemplate(
						routeId,
						origin.PoiId,
						destination.PoiId,
						origin.Id,
						destination.Id,
						segmentIds));
				pairRouteIds.Add(routeId);
			}
		}

		return new RouteTopology(
			new ReadOnlyDictionary<string, LaneSegment>(segments),
			new ReadOnlyDictionary<string, RouteTemplate>(routes),
			new ReadOnlyDictionary<RoutePair, IReadOnlyList<string>>(
				routeIdsByPair.ToDictionary(
					entry => entry.Key,
					entry => (IReadOnlyList<string>)entry.Value.ToArray())));
	}

	public static RoutePair[] HubPairs(string hubPoiId, IReadOnlyCollection<string> spokePoiIds)
	{
		ArgumentException.ThrowIfNullOrEmpty(hubPoiId);
		ArgumentNullException.ThrowIfNull(spokePoiIds);

		return spokePoiIds
			.OrderBy(poiId => poiId, StringComparer.Ordinal)
			.SelectMany(spokePoiId => new[]
			{
				new RoutePair(spokePoiId, hubPoiId),
				new RoutePair(hubPoiId, spokePoiId),
			})
			.ToArray();
	}

	private static IReadOnlyCollection<RoutePair> AllDirectedPairs(IReadOnlyCollection<Dock> docks)
	{
		var orderedDocks = docks
			.OrderBy(dock => dock.PoiId, StringComparer.Ordinal)
			.ToArray();
		var pairs = new List<RoutePair>();

		foreach (var origin in orderedDocks)
		{
			foreach (var destination in orderedDocks)
			{
				if (origin.PoiId == destination.PoiId)
					continue;

				pairs.Add(new RoutePair(origin.PoiId, destination.PoiId));
			}
		}

		return pairs;
	}

	private static IReadOnlyList<Coord> GenerateCruisePath(
		int seed,
		int width,
		int height,
		Dock origin,
		Dock destination,
		Coord start,
		int variant,
		int variantCount,
		IReadOnlyCollection<CircularExclusion> exclusions,
		IReadOnlyList<Dock> docks,
		IReadOnlyList<AcceptedPath> accepted)
	{
		var centeredVariant = variant - (variantCount - 1) * 0.5;
		var corridorOffset = DirectionalBandOffset + centeredVariant * VariantBandSpacing;
		var pair = new RoutePair(origin.PoiId, destination.PoiId);

		for (var attempt = 0; attempt < MaxGenerationAttempts; attempt++)
		{
			var random = new StableRandom(StableSeed(
				seed,
				origin.PoiId,
				destination.PoiId,
				variant,
				attempt));

			// Retrying changes the curve without making it progressively noisier.
			var amplitude = BaseWaveAmplitude * (0.75 + random.NextDouble() * 0.50);
			var path = BuildSmoothPath(
				start,
				destination.QueueHold,
				corridorOffset,
				amplitude,
				ref random);

			if (!IsInsideBounds(path, width, height))
				continue;
			if (TouchesExclusion(path, exclusions))
				continue;
			if (ConflictsWithAcceptedPath(path, pair, accepted, docks))
				continue;

			return path;
		}

		throw new InvalidOperationException(
			$"Could not generate clear route {origin.PoiId} -> {destination.PoiId} "
			+ $"variant {variant} after {MaxGenerationAttempts} attempts. "
			+ "The requested fully connected topology may not fit in the available 2D space; "
			+ "increase corridor space, reduce route variants, or model shared intersections.");
	}

	private static IReadOnlyList<Coord> BuildSmoothPath(
		Coord start,
		Coord end,
		double corridorOffset,
		double waveAmplitude,
		ref StableRandom random)
	{
		var dx = end.X - start.X;
		var dz = end.Z - start.Z;
		var distance = System.Math.Sqrt(dx * dx + dz * dz);
		if (distance < 1.0)
			throw new InvalidOperationException($"Route cruise section is too short: {start} -> {end}.");

		var normalX = -dz / distance;
		var normalZ = dx / distance;
		var phase1 = random.NextDouble() * System.Math.Tau;
		var phase2 = random.NextDouble() * System.Math.Tau;
		var sampleCount = System.Math.Max(8, (int)System.Math.Ceiling(distance / SampleSpacing));
		var points = new List<Coord>(sampleCount + 1);

		for (var i = 0; i <= sampleCount; i++)
		{
			var t = i / (double)sampleCount;
			var envelope = System.Math.Pow(System.Math.Sin(System.Math.PI * t), 2.0);
			var wave =
				0.70 * System.Math.Sin(System.Math.Tau * t + phase1)
				+ 0.30 * System.Math.Sin(2.0 * System.Math.Tau * t + phase2);
			var lateral = envelope * (corridorOffset + waveAmplitude * wave);

			var point = new Coord(
				(int)System.Math.Round(start.X + dx * t + normalX * lateral),
				0,
				(int)System.Math.Round(start.Z + dz * t + normalZ * lateral));

			if (points.Count == 0 || points[^1] != point)
				points.Add(point);
		}

		points[0] = start;
		points[^1] = end;
		return points;
	}

	private static Coord BuildDepartureThroatEnd(
		Dock origin,
		Dock destination,
		int width,
		int height,
		IReadOnlyCollection<CircularExclusion> exclusions)
	{
		var start = origin.DepartureBerth;
		var outward = UnitVector(
			origin.QueueHold.X - origin.ArrivalBerth.X,
			origin.QueueHold.Z - origin.ArrivalBerth.Z);
		var towardDestination = UnitVector(
			destination.QueueHold.X - start.X,
			destination.QueueHold.Z - start.Z);
		var direction = UnitVector(
			outward.X * 0.65 + towardDestination.X * 0.35,
			outward.Z * 0.65 + towardDestination.Z * 0.35);

		var length = DepartureThroatLength;
		for (var attempt = 0; attempt < 8; attempt++)
		{
			var end = new Coord(
				(int)System.Math.Round(start.X + direction.X * length),
				0,
				(int)System.Math.Round(start.Z + direction.Z * length));
			var candidate = new[] { start, end };
			if (IsInsideBounds(candidate, width, height)
				&& !TouchesExclusion(candidate, exclusions))
			{
				return end;
			}

			length *= 0.75;
		}

		throw new InvalidOperationException(
			$"Could not place departure throat for {origin.PoiId} -> {destination.PoiId}.");
	}

	private static IReadOnlyList<IReadOnlyList<Coord>> SplitIntoReservationBlocks(
		IReadOnlyList<Coord> path,
		double targetLength)
	{
		if (path.Count < 2)
			throw new ArgumentException("A route path requires at least two points.", nameof(path));

		var result = new List<IReadOnlyList<Coord>>();
		var current = new List<Coord> { path[0] };
		var length = 0.0;

		for (var i = 1; i < path.Count; i++)
		{
			length += Distance(path[i - 1], path[i]);
			current.Add(path[i]);

			if (length < targetLength || i == path.Count - 1)
				continue;

			result.Add(current.ToArray());
			current = [path[i]];
			length = 0.0;
		}

		if (current.Count > 1)
			result.Add(current.ToArray());

		return result;
	}

	private static bool TouchesExclusion(
		IReadOnlyList<Coord> path,
		IReadOnlyCollection<CircularExclusion> exclusions)
	{
		foreach (var exclusion in exclusions)
		{
			var forbiddenDistance = exclusion.Radius + ExclusionClearance;
			for (var i = 1; i < path.Count; i++)
			{
				if (PointToSegmentDistance(exclusion.Center, path[i - 1], path[i])
					< forbiddenDistance)
				{
					return true;
				}
			}
		}

		return false;
	}

	private static bool ConflictsWithAcceptedPath(
		IReadOnlyList<Coord> candidate,
		RoutePair candidatePair,
		IReadOnlyList<AcceptedPath> accepted,
		IReadOnlyList<Dock> docks)
	{
		foreach (var existing in accepted)
		{
			if (existing.Pair == candidatePair)
				continue;
			if (existing.Pair.OriginPoiId == candidatePair.DestinationPoiId
				&& existing.Pair.DestinationPoiId == candidatePair.OriginPoiId)
				continue;

			for (var a = 1; a < candidate.Count; a++)
			{
				for (var b = 1; b < existing.Points.Count; b++)
				{
					var candidateStart = candidate[a - 1];
					var candidateEnd = candidate[a];
					var existingStart = existing.Points[b - 1];
					var existingEnd = existing.Points[b];

					if (MayConvergeAtDock(
						candidateStart,
						candidateEnd,
						existingStart,
						existingEnd,
						docks))
					{
						continue;
					}

					if (SegmentDistance(
						candidateStart,
						candidateEnd,
						existingStart,
						existingEnd) < MinimumLaneClearance)
					{
						return true;
					}
				}
			}
		}

		return false;
	}

	private static bool MayConvergeAtDock(
		Coord a1,
		Coord a2,
		Coord b1,
		Coord b2,
		IReadOnlyList<Dock> docks)
	{
		foreach (var dock in docks)
		{
			if (NearDock(a1, a2, dock) && NearDock(b1, b2, dock))
				return true;
		}

		return false;
	}

	private static bool NearDock(Coord a, Coord b, Dock dock)
	{
		var midpoint = new Coord((a.X + b.X) / 2, 0, (a.Z + b.Z) / 2);
		return Distance(midpoint, dock.ArrivalBerth) <= DockMergeRadius
			|| Distance(midpoint, dock.DepartureBerth) <= DockMergeRadius
			|| Distance(midpoint, dock.QueueHold) <= DockMergeRadius;
	}

	private static double SegmentDistance(Coord a1, Coord a2, Coord b1, Coord b2)
	{
		if (SegmentsIntersect(a1, a2, b1, b2))
			return 0.0;

		return System.Math.Min(
			System.Math.Min(PointToSegmentDistance(a1, b1, b2), PointToSegmentDistance(a2, b1, b2)),
			System.Math.Min(PointToSegmentDistance(b1, a1, a2), PointToSegmentDistance(b2, a1, a2)));
	}

	private static bool SegmentsIntersect(Coord a1, Coord a2, Coord b1, Coord b2)
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

	private static double Orientation(Coord a, Coord b, Coord c) =>
		(b.X - a.X) * (double)(c.Z - a.Z) - (b.Z - a.Z) * (double)(c.X - a.X);

	private static bool OnSegment(Coord a, Coord b, Coord p) =>
		p.X >= System.Math.Min(a.X, b.X) && p.X <= System.Math.Max(a.X, b.X)
		&& p.Z >= System.Math.Min(a.Z, b.Z) && p.Z <= System.Math.Max(a.Z, b.Z);

	private static double PointToSegmentDistance(Coord point, Coord start, Coord end)
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

	private static bool IsInsideBounds(IReadOnlyList<Coord> points, int width, int height) =>
		points.All(point =>
			point.Y == 0
			&& point.X >= 0 && point.X < width
			&& point.Z >= 0 && point.Z < height);

	private static void ValidateDocks(IReadOnlyList<Dock> docks, int width, int height)
	{
		var dockIds = new HashSet<string>(StringComparer.Ordinal);
		var poiIds = new HashSet<string>(StringComparer.Ordinal);
		foreach (var dock in docks)
		{
			if (!dockIds.Add(dock.Id))
				throw new ArgumentException($"Duplicate dock id '{dock.Id}'.", nameof(docks));
			if (!poiIds.Add(dock.PoiId))
				throw new ArgumentException($"Multiple docks for POI '{dock.PoiId}'.", nameof(docks));

			if (!IsInsideBounds(
				[dock.ArrivalBerth, dock.DepartureBerth, dock.QueueHold],
				width,
				height))
			{
				throw new ArgumentException($"Dock '{dock.Id}' has an out-of-bounds feature.", nameof(docks));
			}
		}
	}

	private static void AddSegment(
		IDictionary<string, LaneSegment> segments,
		string id,
		IReadOnlyList<Coord> points)
	{
		var normalized = points
			.Where((point, index) => index == 0 || point != points[index - 1])
			.ToArray();
		if (normalized.Length < 2)
			throw new InvalidOperationException($"Lane segment '{id}' has fewer than two distinct points.");
		if (!segments.TryAdd(id, new LaneSegment(id, normalized)))
			throw new InvalidOperationException($"Duplicate lane segment id '{id}'.");
	}

	private static (double X, double Z) UnitVector(double x, double z)
	{
		var length = System.Math.Sqrt(x * x + z * z);
		if (length <= 0.000001)
			return (1.0, 0.0);
		return (x / length, z / length);
	}

	private static double Distance(Coord a, Coord b)
	{
		var dx = b.X - a.X;
		var dz = b.Z - a.Z;
		return System.Math.Sqrt(dx * (double)dx + dz * (double)dz);
	}

	private static string RouteId(string originPoiId, string destinationPoiId, int variant) =>
		$"route:{originPoiId}:{destinationPoiId}:v{variant}";

	private static string SegmentId(string destinationPoiId, string suffix) =>
		$"lane:{destinationPoiId}:{suffix}";

	private static ulong StableSeed(
		int seed,
		string originPoiId,
		string destinationPoiId,
		int variant,
		int attempt)
	{
		const ulong offset = 14695981039346656037UL;
		const ulong prime = 1099511628211UL;
		var hash = offset;

		MixInt(seed);
		MixString(originPoiId);
		MixString(destinationPoiId);
		MixInt(variant);
		MixInt(attempt);
		return hash;

		void MixInt(int value)
		{
			for (var shift = 0; shift < 32; shift += 8)
			{
				hash ^= (byte)(value >> shift);
				hash *= prime;
			}
		}

		void MixString(string value)
		{
			foreach (var character in value)
			{
				hash ^= (byte)character;
				hash *= prime;
				hash ^= (byte)(character >> 8);
				hash *= prime;
			}
		}
	}

	private sealed record AcceptedPath(RoutePair Pair, IReadOnlyList<Coord> Points);

	private struct StableRandom(ulong state)
	{
		private ulong _state = state;

		public double NextDouble() =>
			(NextUInt64() >> 11) * (1.0 / (1UL << 53));

		private ulong NextUInt64()
		{
			_state += 0x9E3779B97F4A7C15UL;
			var value = _state;
			value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
			value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
			return value ^ (value >> 31);
		}
	}
}
