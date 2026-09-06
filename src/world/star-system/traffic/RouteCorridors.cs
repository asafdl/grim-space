using System.Collections.ObjectModel;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Poi;

namespace GrimSpace.World.StarSystem.Traffic;

public readonly record struct RoutePair(string DockAId, string DockBId);

public static class RouteCorridors
{
	public static IReadOnlyDictionary<string, SpaceRoute> Build(
		int width,
		int height,
		IReadOnlyCollection<Dock> docks,
		IReadOnlyCollection<RoutePair> pairs,
		IReadOnlyCollection<PointOfInterest> pois,
		int halfWidth)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(width, 2);
		ArgumentOutOfRangeException.ThrowIfLessThan(height, 2);
		ArgumentOutOfRangeException.ThrowIfLessThan(halfWidth, 1);
		ArgumentNullException.ThrowIfNull(pairs);
		ArgumentNullException.ThrowIfNull(pois);

		var orderedDocks = docks
			.OrderBy(dock => dock.Id, StringComparer.Ordinal)
			.ToArray();
		ValidateDocks(orderedDocks, width, height);

		var docksById = orderedDocks.ToDictionary(dock => dock.Id, StringComparer.Ordinal);
		var canonicalPairs = CanonicalizePairs(pairs, docksById);
		var cells = Enumerable.Repeat(PathfindingCell.OpenSpace, width * height).ToArray();

		foreach (var poi in pois)
		{
			GridRaster.FillCircle(
				width,
				height,
				poi.PlacedCenter,
				poi.RouteExclusionRadius,
				(x, z) => cells[new Coord(x, 0, z).ToIndex(width)] = PathfindingCell.Obstacle);
		}

		foreach (var dock in orderedDocks)
			cells[dock.Position.ToIndex(width)] = PathfindingCell.OpenSpace;

		var routes = new Dictionary<string, SpaceRoute>(StringComparer.Ordinal);

		foreach (var pair in canonicalPairs)
		{
			var dockA = docksById[pair.DockAId];
			var dockB = docksById[pair.DockBId];
			var terrain = PathfindingTerrain.FromCells(width, height, cells);
			var pathfinder = new AStarPathfinder(terrain);
			var result = pathfinder.FindPath(dockA.Position, dockB.Position);
			if (result is not PathfindingResult.Found found)
			{
				throw new InvalidOperationException(
					$"No path between docks '{pair.DockAId}' and '{pair.DockBId}'.");
			}

			var centerline = FlattenCenterline(found.Path);
			var routeId = RouteId(pair.DockAId, pair.DockBId);
			routes[routeId] = new SpaceRoute(
				routeId,
				pair.DockAId,
				pair.DockBId,
				centerline,
				halfWidth);

			GridRaster.StampCorridor(
				width,
				height,
				centerline,
				halfWidth,
				(x, z) => cells[new Coord(x, 0, z).ToIndex(width)] = PathfindingCell.RouteCorridor);
		}

		return new ReadOnlyDictionary<string, SpaceRoute>(routes);
	}

	private static IReadOnlyList<Coord> FlattenCenterline(TransitPath path)
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

		return points;
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
