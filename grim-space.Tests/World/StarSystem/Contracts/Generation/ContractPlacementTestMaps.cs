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
using GrimSpace.World.StarSystem.Poi.Concrete;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Traffic;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.Tests.World.StarSystem.Contracts.Generation;

internal static class ContractPlacementTestMaps
{
	public const string IssuerAId = "issuer-a";
	public const string IssuerBId = "issuer-b";

	public static StarMap TwoIssuers(int seed = 42)
	{
		var poiA = CreateIssuerPoi(IssuerAId, new Coord(128, 0, 256));
		var poiB = CreateIssuerPoi(IssuerBId, new Coord(384, 0, 256));
		var pois = new PointOfInterest[] { poiA, poiB };
		var mapSize = 512;
		var cells = Enumerable.Repeat(PathfindingCell.OpenSpace, mapSize * mapSize).ToArray();
		var terrain = PathfindingTerrain.FromCells(mapSize, mapSize, cells);
		var blueprint = new StarSystemBlueprint(
			seed,
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

	private static PointOfInterest CreateIssuerPoi(string poiId, Coord center)
	{
		var facilityId = Facility.ScopedId(poiId, AdministrativeCore.ManagementFacilitySlug);
		return new TestIssuerPoi(
			poiId,
			center,
			[
				new Facility(
					facilityId,
					"Contracts Desk",
					EPresentationAnchor.Management,
					AdministrativeCore.ManagementScenePath,
					[
						new FacilityOperator(
							"contracts",
							EFacilityOperatorRole.Contracts,
							AdministrativeCore.ContractOperatorSceneSlotId),
					]),
			]);
	}

	private sealed class TestIssuerPoi : PointOfInterest
	{
		public TestIssuerPoi(string id, Coord center, IReadOnlyList<Facility> facilities)
			: base(id, id, 32, EPoiLogicalRole.Administrative, center, facilities: facilities)
		{
		}

		public override string DockNeighbourPoiId(SupplySystemPlan plan) => Id;

		public override PointOfInterest Fork() => new TestIssuerPoi(Id, PlacedCenter, Facilities);

		protected override PointOfInterest WithCenter(Coord center) => new TestIssuerPoi(Id, center, Facilities);
	}
}
