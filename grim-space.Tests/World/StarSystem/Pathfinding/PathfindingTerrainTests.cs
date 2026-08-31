using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Traffic;

namespace GrimSpace.Tests.World.StarSystem.Pathfinding;

public sealed class PathfindingTerrainTests
{
	[Fact]
	public void Create_StampsOpenSpaceRoutesObstaclesAndUnblocksDocks()
	{
		var docks = new[]
		{
			new Dock("dock:a", "poi-a", new Coord(5, 0, 5)),
			new Dock("dock:b", "poi-b", new Coord(20, 0, 5)),
		};
		var routes = new Dictionary<string, SpaceRoute>
		{
			["route:test"] = new(
				"route:test",
				"dock:a",
				"dock:b",
				[new Coord(5, 0, 5), new Coord(20, 0, 5)],
				2),
		};
		var pois = new TestPoi("poi-b", new Coord(20, 0, 12), 4);

		var terrain = PathfindingTerrain.Create(32, 32, routes.Values, [pois], docks);

		Assert.False(terrain[10, 5].Blocked);
		Assert.Equal(1.5, terrain[10, 5].SpeedMultiplier);
		Assert.True(terrain[20, 12].Blocked);
		Assert.False(terrain[20, 5].Blocked);
		Assert.Equal(1.5, terrain[1, 1].WeightScale);
	}

	private sealed class TestPoi : GrimSpace.World.StarSystem.Poi.PointOfInterest
	{
		public TestPoi(string id, Coord center, int radius)
			: base(id, id, radius, GrimSpace.World.StarSystem.Poi.EPoiLogicalRole.Environment, center)
		{
		}

		public override string DockNeighbourPoiId(GrimSpace.World.StarSystem.Generation.SupplySystemPlan plan) =>
			"poi-a";

		public override GrimSpace.World.StarSystem.Poi.PointOfInterest Fork() =>
			new TestPoi(Id, PlacedCenter, Radius);

		protected override GrimSpace.World.StarSystem.Poi.PointOfInterest WithCenter(Coord center) =>
			new TestPoi(Id, center, Radius);
	}
}
