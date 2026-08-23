using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Traffic;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem;

public sealed class StarMap : IWorld<StarMap>, IActorStateWorld<State, StarMap>
{
	public const int DevMapWidth = 1024;
	public const int DevMapHeight = 1024;
	public const int DevRouteHalfWidth = 24;

	public StarSystemBlueprint Blueprint { get; }
	public int Seed => Blueprint.Seed;
	public int Width => Blueprint.Width;
	public int Height => Blueprint.Height;
	public IReadOnlyList<PointOfInterest> PointsOfInterest { get; }
	public Timeline Timeline { get; }
	public IReadOnlyDictionary<string, Dock> DocksById { get; }
	public IReadOnlyDictionary<string, Dock> DocksByPoiId { get; }
	public IReadOnlyDictionary<string, SpaceRoute> RoutesById { get; }
	public UnitRegistry UnitRegistry { get; }
	public SystemTrafficController TrafficController { get; }

	public State StateOf(string unitId) => UnitRegistry.UnitOf(unitId).State;

	internal StarMap(
		StarSystemBlueprint blueprint,
		IReadOnlyList<PointOfInterest> pointsOfInterest,
		Timeline timeline,
		IReadOnlyDictionary<string, Dock> docksById,
		IReadOnlyDictionary<string, Dock> docksByPoiId,
		IReadOnlyDictionary<string, SpaceRoute> routesById,
		UnitRegistry unitRegistry,
		SystemTrafficController trafficController)
	{
		Blueprint = blueprint;
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
			Blueprint,
			PointsOfInterest,
			Timeline.Clone(),
			DocksById,
			DocksByPoiId,
			RoutesById,
			UnitRegistry.CloneForFork(),
			TrafficController.Fork());

	public static StarMap CreateDevDefault(int seed = 0) =>
		StarSystemGenerator.Generate(seed, EStarSystemClass.Supply);
}
