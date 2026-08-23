using System.Collections.ObjectModel;
using GrimSpace.Math;
using GrimSpace.Math.Grid;
using GrimSpace.Math.Routes;

namespace GrimSpace.World.StarSystem.Traffic;

public readonly record struct RoutePair(string DockAId, string DockBId);

public sealed record CircularExclusion(Coord Center, int Radius, string? PoiId = null);

public static class RouteBuilder
{
	private const double SampleSpacing = 16.0;
	private const double BaseWaveAmplitude = 9.0;
	private const double ExclusionClearance = 6.0;
	private const int MaxGenerationAttempts = 64;

	public static IReadOnlyDictionary<string, SpaceRoute> Build(
		int seed,
		int width,
		int height,
		IReadOnlyCollection<Dock> docks,
		IReadOnlyCollection<RoutePair> pairs,
		IReadOnlyCollection<CircularExclusion> exclusions,
		int halfWidth)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(width, 2);
		ArgumentOutOfRangeException.ThrowIfLessThan(height, 2);
		ArgumentOutOfRangeException.ThrowIfLessThan(halfWidth, 1);
		ArgumentNullException.ThrowIfNull(pairs);

		var orderedDocks = docks
			.OrderBy(dock => dock.Id, StringComparer.Ordinal)
			.ToArray();

		ValidateDocks(orderedDocks, width, height);

		var docksById = orderedDocks.ToDictionary(dock => dock.Id, StringComparer.Ordinal);
		var canonicalPairs = CanonicalizePairs(pairs, docksById);
		var routes = new Dictionary<string, SpaceRoute>(StringComparer.Ordinal);

		foreach (var pair in canonicalPairs)
		{
			var dockA = docksById[pair.DockAId];
			var dockB = docksById[pair.DockBId];
			var centerline = GenerateCenterline(
				seed,
				width,
				height,
				dockA,
				dockB,
				pair,
				exclusions,
				halfWidth);

			var routeId = RouteId(pair.DockAId, pair.DockBId);
			routes[routeId] = new SpaceRoute(
				routeId,
				pair.DockAId,
				pair.DockBId,
				centerline,
				halfWidth);
		}

		return new ReadOnlyDictionary<string, SpaceRoute>(routes);
	}

	public static RoutePair[] HubPairs(string hubDockId, IReadOnlyCollection<string> spokeDockIds)
	{
		ArgumentException.ThrowIfNullOrEmpty(hubDockId);
		ArgumentNullException.ThrowIfNull(spokeDockIds);

		return spokeDockIds
			.OrderBy(dockId => dockId, StringComparer.Ordinal)
			.Select(spokeDockId => CanonicalPair(hubDockId, spokeDockId))
			.ToArray();
	}

	private static IReadOnlyList<RoutePair> CanonicalizePairs(
		IReadOnlyCollection<RoutePair> pairs,
		IReadOnlyDictionary<string, Dock> docksById)
	{
		var seen = new HashSet<(string, string)>();
		var canonicalPairs = new List<RoutePair>();

		foreach (var pair in pairs)
		{
			if (!docksById.ContainsKey(pair.DockAId))
				throw new ArgumentException($"Unknown dock '{pair.DockAId}'.", nameof(pairs));
			if (!docksById.ContainsKey(pair.DockBId))
				throw new ArgumentException($"Unknown dock '{pair.DockBId}'.", nameof(pairs));
			if (pair.DockAId == pair.DockBId)
				throw new ArgumentException($"Route pair cannot reference the same dock: {pair}.", nameof(pairs));

			var canonical = CanonicalPair(pair.DockAId, pair.DockBId);
			if (!seen.Add((canonical.DockAId, canonical.DockBId)))
				throw new ArgumentException($"Duplicate route pair: {pair}.", nameof(pairs));

			canonicalPairs.Add(canonical);
		}

		return canonicalPairs
			.OrderBy(pair => pair.DockAId, StringComparer.Ordinal)
			.ThenBy(pair => pair.DockBId, StringComparer.Ordinal)
			.ToArray();
	}

	private static RoutePair CanonicalPair(string dockAId, string dockBId) =>
		string.Compare(dockAId, dockBId, StringComparison.Ordinal) <= 0
			? new RoutePair(dockAId, dockBId)
			: new RoutePair(dockBId, dockAId);

	private static string RouteId(string dockAId, string dockBId) =>
		$"route:{dockAId}:{dockBId}";

	private static IReadOnlyList<Coord> GenerateCenterline(
		int seed,
		int width,
		int height,
		Dock dockA,
		Dock dockB,
		RoutePair pair,
		IReadOnlyCollection<CircularExclusion> exclusions,
		int halfWidth)
	{
		for (var attempt = 0; attempt < MaxGenerationAttempts; attempt++)
		{
			var random = new StableRandom(
				StableSeedMixer.From(seed)
					.Add(pair.DockAId)
					.Add(pair.DockBId)
					.Add(attempt)
					.Value);
			var amplitude = BaseWaveAmplitude * (0.75 + random.NextDouble() * 0.50);
			var path = BuildSmoothPath(dockA.Position, dockB.Position, amplitude, ref random);

			if (!CorridorInsideBounds(path, width, height, halfWidth))
				continue;
			if (CorridorTouchesExclusion(path, exclusions, halfWidth, dockA.PoiId, dockB.PoiId))
				continue;

			return path;
		}

		throw new InvalidOperationException(
			$"Could not generate route {pair.DockAId} <-> {pair.DockBId} "
			+ $"after {MaxGenerationAttempts} attempts.");
	}

	private static IReadOnlyList<Coord> BuildSmoothPath(
		Coord start,
		Coord end,
		double waveAmplitude,
		ref StableRandom random)
	{
		var dx = end.X - start.X;
		var dz = end.Z - start.Z;
		var distance = System.Math.Sqrt(dx * dx + dz * dz);
		if (distance < 1.0)
			throw new InvalidOperationException($"Route is too short: {start} -> {end}.");

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
			var lateral = envelope * waveAmplitude * wave;

			var point = new Coord(
				(int)System.Math.Round(start.X + dx * t + normalX * lateral),
				0,
				(int)System.Math.Round(start.Z + dz * t + normalZ * lateral));

			if (points.Count == 0 || points[^1] != point)
				points.Add(point);
		}

		points[0] = start;
		points[^1] = end;
		return RouteGeometry.NormalizePolyline(points);
	}

	private static bool CorridorInsideBounds(
		IReadOnlyList<Coord> centerline,
		int width,
		int height,
		int halfWidth)
	{
		foreach (var point in CorridorSamplePoints(centerline, halfWidth))
		{
			if (point.Y != 0
				|| point.X < 0 || point.X >= width
				|| point.Z < 0 || point.Z >= height)
			{
				return false;
			}
		}

		return true;
	}

	private static bool CorridorTouchesExclusion(
		IReadOnlyList<Coord> centerline,
		IReadOnlyCollection<CircularExclusion> exclusions,
		int halfWidth,
		string endpointPoiAId,
		string endpointPoiBId)
	{
		foreach (var exclusion in exclusions)
		{
			if (exclusion.PoiId is not null
				&& (exclusion.PoiId == endpointPoiAId || exclusion.PoiId == endpointPoiBId))
			{
				continue;
			}

			var forbiddenDistance = exclusion.Radius + halfWidth + ExclusionClearance;
			for (var i = 1; i < centerline.Count; i++)
			{
				if (RouteGeometry.PointToSegmentDistance(exclusion.Center, centerline[i - 1], centerline[i])
					< forbiddenDistance)
				{
					return true;
				}
			}
		}

		return false;
	}

	private static IEnumerable<Coord> CorridorSamplePoints(IReadOnlyList<Coord> centerline, int halfWidth)
	{
		for (var i = 0; i < centerline.Count; i++)
		{
			var tangent = CorridorTangent(centerline, i);
			var perpendicular = (-tangent.Z, tangent.X);
			yield return Offset(centerline[i], perpendicular, halfWidth);
			yield return Offset(centerline[i], perpendicular, -halfWidth);
			yield return centerline[i];
		}
	}

	private static (double X, double Z) CorridorTangent(IReadOnlyList<Coord> centerline, int index)
	{
		if (centerline.Count < 2)
			return (1.0, 0.0);

		var previous = centerline[System.Math.Max(0, index - 1)];
		var next = centerline[System.Math.Min(centerline.Count - 1, index + 1)];
		return RouteGeometry.UnitVector(next.X - previous.X, next.Z - previous.Z);
	}

	private static Coord Offset(Coord origin, (double X, double Z) direction, double distance) =>
		new(
			(int)System.Math.Round(origin.X + direction.X * distance),
			0,
			(int)System.Math.Round(origin.Z + direction.Z * distance));

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

			if (!IsInsideBounds(dock.Position, width, height))
				throw new ArgumentException($"Dock '{dock.Id}' is out of bounds.", nameof(docks));
		}
	}

	private static bool IsInsideBounds(Coord point, int width, int height) =>
		point.Y == 0
		&& point.X >= 0 && point.X < width
		&& point.Z >= 0 && point.Z < height;
}
