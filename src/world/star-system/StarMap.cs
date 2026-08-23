using GrimSpace.Core.Engine;
using GrimSpace.Core.Ids;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Traffic;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem;

public sealed class StarMap : IWorld<StarMap>, IActorStateWorld<State, StarMap>
{
	public const int DevMapWidth = 1024;
	public const int DevMapHeight = 1024;
	private const int DevLayoutScale = 32;
	public const int DevRouteHalfWidth = 24;

	public int Seed { get; }
	public int Width { get; }
	public int Height { get; }
	public IReadOnlyList<PointOfInterest> PointsOfInterest { get; }
	public Timeline Timeline { get; }
	public IReadOnlyDictionary<string, Dock> DocksById { get; }
	public IReadOnlyDictionary<string, Dock> DocksByPoiId { get; }
	public IReadOnlyDictionary<string, SpaceRoute> RoutesById { get; }
	public UnitRegistry UnitRegistry { get; }
	public SystemTrafficController TrafficController { get; }

	public State StateOf(string unitId) => UnitRegistry.UnitOf(unitId).State;

	private StarMap(
		int seed,
		int width,
		int height,
		IReadOnlyList<PointOfInterest> pointsOfInterest,
		Timeline timeline,
		IReadOnlyDictionary<string, Dock> docksById,
		IReadOnlyDictionary<string, Dock> docksByPoiId,
		IReadOnlyDictionary<string, SpaceRoute> routesById,
		UnitRegistry unitRegistry,
		SystemTrafficController trafficController)
	{
		Seed = seed;
		Width = width;
		Height = height;
		PointsOfInterest = pointsOfInterest;
		Timeline = timeline;
		DocksById = docksById;
		DocksByPoiId = docksByPoiId;
		RoutesById = routesById;
		UnitRegistry = unitRegistry;
		TrafficController = trafficController;
	}

	public bool IsInBounds(Coord point) =>
		point.Y == 0
		&& point.X >= 0 && point.X < Width
		&& point.Z >= 0 && point.Z < Height;

	public bool TryResolveRoute(
		string fromDockId,
		string toDockId,
		out SpaceRoute route,
		out bool towardDockB) =>
		SystemTrafficController.TryGetRoute(RoutesById, fromDockId, toDockId, out route, out towardDockB);

	public static bool PoisOverlap(PointOfInterest a, PointOfInterest b)
	{
		var dx = a.Center.X - b.Center.X;
		var dz = a.Center.Z - b.Center.Z;
		var distanceSquared = (long)dx * dx + (long)dz * dz;
		var combined = a.Radius + b.Radius;
		return distanceSquared < (long)combined * combined;
	}

	public StarMap Fork() =>
		new(
			Seed,
			Width,
			Height,
			PointsOfInterest,
			Timeline.Clone(),
			DocksById,
			DocksByPoiId,
			RoutesById,
			UnitRegistry.CloneForFork(),
			TrafficController.Fork());

	public static StarMap CreateDevDefault(int seed = 0)
	{
		var pois = new PointOfInterest[]
		{
			DevPoi("star-dev", EPointOfInterestKind.Star, 14, 14, 4, 4),
			DevPoi("planet-dev-a", EPointOfInterestKind.Planet, 4, 6, 2, 2),
			DevPoi("planet-dev-b", EPointOfInterestKind.Planet, 22, 20, 2, 2),
			DevPoi("station-dev", EPointOfInterestKind.Station, 24, 5, 1, 1),
		};

		var station = pois.First(p => p.Kind == EPointOfInterestKind.Station);
		var star = pois.FirstOrDefault(p => p.Kind == EPointOfInterestKind.Star);

		var docksById = new Dictionary<string, Dock>(StringComparer.Ordinal);
		var docksByPoiId = new Dictionary<string, Dock>(StringComparer.Ordinal);
		var dockIds = new TypedIdGenerator();
		foreach (var poi in pois.Where(p => p.Kind != EPointOfInterestKind.Star))
		{
			var dock = DockLayout.CreateDock(dockIds, poi, station, star);
			docksById[dock.Id] = dock;
			docksByPoiId[poi.Id] = dock;
		}

		var stationDock = docksByPoiId[station.Id];
		var planetADock = docksByPoiId["planet-dev-a"];
		var planetBDock = docksByPoiId["planet-dev-b"];
		var spokeDockIds = docksByPoiId.Values
			.Where(dock => dock.PoiId != station.Id)
			.Select(dock => dock.Id)
			.ToArray();
		var routePairs = RouteBuilder.HubPairs(stationDock.Id, spokeDockIds);

		var exclusions = pois
			.Where(poi => poi.Kind == EPointOfInterestKind.Star)
			.Select(poi => new CircularExclusion(poi.Center, poi.Radius + 30))
			.ToArray();

		var routesById = RouteBuilder.Build(
			seed,
			DevMapWidth,
			DevMapHeight,
			docksById.Values,
			routePairs,
			exclusions,
			DevRouteHalfWidth);

		var unitRegistry = new UnitRegistry();
		foreach (var unit in Factory.CreateDevUnits(stationDock.Id, planetADock.Id, planetBDock.Id))
			unitRegistry.Add(unit);

		return new StarMap(
			seed,
			DevMapWidth,
			DevMapHeight,
			pois,
			new Timeline(),
			docksById,
			docksByPoiId,
			routesById,
			unitRegistry,
			new SystemTrafficController());
	}

	private static PointOfInterest DevPoi(
		string id,
		EPointOfInterestKind kind,
		int x,
		int z,
		int width,
		int depth)
	{
		var centerX = (x + (width - 1) / 2) * DevLayoutScale;
		var centerZ = (z + (depth - 1) / 2) * DevLayoutScale;
		var radius = System.Math.Max(width, depth) * DevLayoutScale / 2;

		return new PointOfInterest(
			id,
			kind,
			DisplayNameFromId(id),
			new Coord(centerX, 0, centerZ),
			radius);
	}

	private static PointOfInterest DevPoi(
		TypedIdGenerator ids,
		EPointOfInterestKind kind,
		int x,
		int z,
		int width,
		int depth)
	{
		var typeSlug = kind switch
		{
			EPointOfInterestKind.Star => "star",
			EPointOfInterestKind.Planet => "planet",
			EPointOfInterestKind.Station => "station",
			_ => throw new ArgumentOutOfRangeException(nameof(kind)),
		};

		var id = ids.NextId(typeSlug);
		var centerX = (x + (width - 1) / 2) * DevLayoutScale;
		var centerZ = (z + (depth - 1) / 2) * DevLayoutScale;
		var radius = System.Math.Max(width, depth) * DevLayoutScale / 2;

		return new PointOfInterest(
			id,
			kind,
			DisplayNameFromId(id),
			new Coord(centerX, 0, centerZ),
			radius);
	}

	private static string DisplayNameFromId(string id)
	{
		var dash = id.IndexOf('-');
		return dash > 0 && dash < id.Length - 1 ? id[(dash + 1)..] : id;
	}
}
