using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Landmarks;
using GrimSpace.World.StarSystem.Objectives;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Traffic;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.Tests.World.StarSystem.Areas;

internal static class AreaPickerTestMaps
{
	public const string LandmarkAId = "landmark-a";
	public const string LandmarkBId = "landmark-b";
	public const string NavLandmarkCId = "landmark:nav-c:00";
	public const string NavLandmarkAId = "landmark:nav-a:00";
	public const string NavLandmarkBId = "landmark:nav-b:00";

	public static StarMap OpenLandmarkPair(int span, int mapSize = 512)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(span, 1);
		ArgumentOutOfRangeException.ThrowIfLessThan(mapSize, span + 32);

		var margin = (mapSize - span) / 2;
		var poiA = new TestLandmark(LandmarkAId, "Landmark A", new Coord(margin, 0, mapSize / 2));
		var poiB = new TestLandmark(LandmarkBId, "Landmark B", new Coord(margin + span, 0, mapSize / 2));
		var pois = new PointOfInterest[] { poiA, poiB };
		var cells = Enumerable.Repeat(PathfindingCell.OpenSpace, mapSize * mapSize).ToArray();
		var terrain = PathfindingTerrain.FromCells(mapSize, mapSize, cells);
		var blueprint = new StarSystemBlueprint(
			0,
			mapSize,
			mapSize,
			EStarSystemClass.Supply,
			EFaction.TheOptimality,
			SupplySystemPlan.Copper,
			[],
			[],
			NavigationLandmarkGenerationProfile.Disabled);

		return new StarMap(
			blueprint,
			pois,
			[],
			new Timeline(),
			new Dictionary<string, Dock>(StringComparer.Ordinal),
			new Dictionary<string, Dock>(StringComparer.Ordinal),
			new Dictionary<string, SpaceRoute>(StringComparer.Ordinal),
			new FleetRegistry(),
			new ContractRegistry(),
			new StoryObjectiveRegistry(),
			new PlayerResources(),
			terrain);
	}

	public static StarMap OpenPoiAndNavigationLandmark(int span, int mapSize = 512)
	{
		var map = OpenLandmarkPair(span, mapSize);
		var margin = (mapSize - span) / 2;
		var nav = new NavigationLandmark(
			NavLandmarkCId,
			"Nav C",
			ENavigationLandmarkKind.Moonlet,
			new Coord(margin + span / 2, 0, mapSize / 2 + span / 3),
			1,
			7);
		return Rebuild(map, map.PointsOfInterest, [nav]);
	}

	public static StarMap OpenNavigationLandmarkPair(int span, int mapSize = 512)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(span, 1);
		ArgumentOutOfRangeException.ThrowIfLessThan(mapSize, span + 32);

		var margin = (mapSize - span) / 2;
		var landmarks = new NavigationLandmark[]
		{
			new(
				NavLandmarkAId,
				"Nav A",
				ENavigationLandmarkKind.Moonlet,
				new Coord(margin, 0, mapSize / 2),
				1,
				1),
			new(
				NavLandmarkBId,
				"Nav B",
				ENavigationLandmarkKind.Moonlet,
				new Coord(margin + span, 0, mapSize / 2),
				1,
				2),
		};
		var cells = Enumerable.Repeat(PathfindingCell.OpenSpace, mapSize * mapSize).ToArray();
		var terrain = PathfindingTerrain.FromCells(mapSize, mapSize, cells);
		var blueprint = new StarSystemBlueprint(
			0,
			mapSize,
			mapSize,
			EStarSystemClass.Supply,
			EFaction.TheOptimality,
			SupplySystemPlan.Copper,
			[],
			[],
			NavigationLandmarkGenerationProfile.Disabled);

		return new StarMap(
			blueprint,
			[],
			landmarks,
			new Timeline(),
			new Dictionary<string, Dock>(StringComparer.Ordinal),
			new Dictionary<string, Dock>(StringComparer.Ordinal),
			new Dictionary<string, SpaceRoute>(StringComparer.Ordinal),
			new FleetRegistry(),
			new ContractRegistry(),
			new StoryObjectiveRegistry(),
			new PlayerResources(),
			terrain);
	}

	private static StarMap Rebuild(
		StarMap map,
		IReadOnlyList<PointOfInterest> pois,
		IReadOnlyList<NavigationLandmark> landmarks) =>
		new(
			map.Blueprint,
			pois,
			landmarks,
			map.Timeline,
			map.DocksById,
			map.DocksByPoiId,
			map.RoutesById,
			map.FleetRegistry,
			map.ContractRegistry,
			map.StoryObjectives,
			map.PlayerResources,
			map.PathfindingTerrain);

	private sealed class TestLandmark : PointOfInterest
	{
		public TestLandmark(string id, string displayName, Coord center)
			: base(id, displayName, 1, EPoiLogicalRole.Environment, center)
		{
		}

		public override int RouteExclusionRadius => 0;

		public override string DockNeighbourPoiId(SupplySystemPlan plan) => LandmarkBId;

		public override PointOfInterest Fork() => new TestLandmark(Id, DisplayName, PlacedCenter);

		protected override PointOfInterest WithCenter(Coord center) => new TestLandmark(Id, DisplayName, center);
	}
}
