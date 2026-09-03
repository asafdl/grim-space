using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Contracts;
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
	public const int DevOfferedContractCount = 1;
	public const int DevContractRewardCredits = 100;

	public StarSystemBlueprint Blueprint { get; }
	public EFaction ControllingFaction => Blueprint.ControllingFaction;
	public int Seed => Blueprint.Seed;
	public int Width => Blueprint.Width;
	public int Height => Blueprint.Height;
	public IReadOnlyList<PointOfInterest> PointsOfInterest { get; }
	public Timeline Timeline { get; }
	public IReadOnlyDictionary<string, Dock> DocksById { get; }
	public IReadOnlyDictionary<string, Dock> DocksByPoiId { get; }
	public IReadOnlyDictionary<Coord, Dock> DocksByPosition { get; }
	public IReadOnlyDictionary<string, SpaceRoute> RoutesById { get; }
	public UnitRegistry UnitRegistry { get; }
	public ContractRegistry ContractRegistry { get; }

	public State StateOf(string unitId) => UnitRegistry.UnitOf(unitId).State;

	internal StarMap(
		StarSystemBlueprint blueprint,
		IReadOnlyList<PointOfInterest> pointsOfInterest,
		Timeline timeline,
		IReadOnlyDictionary<string, Dock> docksById,
		IReadOnlyDictionary<string, Dock> docksByPoiId,
		IReadOnlyDictionary<string, SpaceRoute> routesById,
		UnitRegistry unitRegistry,
		ContractRegistry contractRegistry)
	{
		Blueprint = blueprint;
		PointsOfInterest = pointsOfInterest;
		Timeline = timeline;
		DocksById = docksById;
		DocksByPoiId = docksByPoiId;
		DocksByPosition = docksById.Values
			.GroupBy(dock => dock.Position)
			.ToDictionary(group => group.Key, group => group.Single());
		RoutesById = routesById;
		UnitRegistry = unitRegistry;
		ContractRegistry = contractRegistry;
	}

	public bool IsInBounds(Coord point) =>
		point.Y == 0
		&& point.X >= 0 && point.X < Width
		&& point.Z >= 0 && point.Z < Height;

	public StarMap Fork() =>
		new(
			Blueprint,
			PointsOfInterest.Select(poi => poi.Fork()).ToList(),
			Timeline.Clone(),
			DocksById,
			DocksByPoiId,
			RoutesById,
			UnitRegistry.CloneForFork(),
			ContractRegistry.CloneForFork());

	public static bool PoisOverlap(PointOfInterest a, PointOfInterest b)
	{
		var dx = a.PlacedCenter.X - b.PlacedCenter.X;
		var dz = a.PlacedCenter.Z - b.PlacedCenter.Z;
		var distanceSquared = (long)dx * dx + (long)dz * dz;
		var combined = a.Radius + b.Radius;
		return distanceSquared < (long)combined * combined;
	}

	public static StarMap CreateDevDefault(int seed = 0) =>
		CreateDevBuildResult(seed).Map;

	public static StarSystemBuildResult CreateDevBuildResult(int seed = 0)
	{
		var result = StarSystemGenerator.Generate(seed, EStarSystemClass.Supply);
		SeedDevOfferedContracts(result.Map);
		return result;
	}

	private static void SeedDevOfferedContracts(StarMap map)
	{
		var plan = map.Blueprint.SupplyPlan;
		var issuerPoiId = plan.AdministrativePoiId;
		var landmarkGroups = new[]
		{
			new[] { plan.RefineryPoiId, plan.StoragePoiId },
			new[] { plan.ExtractionPoiId, plan.StoragePoiId },
			new[] { plan.RefineryPoiId, plan.ExitPoiId },
		};
		var distances = new[] { EAreaDistance.Low, EAreaDistance.Med, EAreaDistance.High };
		var location = AreaPicker.Pick(map, landmarkGroups, distances, 2);
		var narrative = ContractNarrative.ForSearch("Route Survey", location);
		var contract = new Contract(
			"contract-1",
			EContractTask.Hunt,
			map.ControllingFaction,
			issuerPoiId,
			location,
			new ContractTerms(DevContractRewardCredits),
			narrative);
		map.ContractRegistry.RegisterOffered(contract);
	}
}
