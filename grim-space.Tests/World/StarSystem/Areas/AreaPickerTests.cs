using GrimSpace.Math.Routes;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Landmarks;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Areas;

[StarSystemTestSuite]
public sealed class AreaPickerTests(StarMapFixture maps)
{
	[Fact]
	public void TryPick_NullMap_Throws()
	{
		Assert.Throws<ArgumentNullException>(() =>
			AreaPicker.TryPick(null!, new AreaPickerArgs(["a", "b", "c"]), out _));
	}

	[Fact]
	public void TryPick_UnknownLandmark_Throws()
	{
		var map = AreaPickerTestMaps.OpenNavigationTriangle(200);
		var args = new AreaPickerArgs(
			[AreaPickerTestMaps.NavLandmarkAId, "poi-missing", AreaPickerTestMaps.NavLandmarkBId]);

		Assert.Throws<ArgumentException>(() => AreaPicker.TryPick(map, args, out _));
	}

	[Fact]
	public void TryPick_ReturnsFalseWithFewerThanThreeCandidates()
	{
		var map = AreaPickerTestMaps.OpenNavigationLandmarkPair(200);
		var args = new AreaPickerArgs(
			[AreaPickerTestMaps.NavLandmarkAId, AreaPickerTestMaps.NavLandmarkBId]);

		Assert.False(AreaPicker.TryPick(map, args, out _));
	}

	[Fact]
	public void TryPick_OnOpenTriangle_EnclosesSearchCircle()
	{
		var map = AreaPickerTestMaps.OpenNavigationTriangle(240);
		var args = new AreaPickerArgs(MapLandmarkQueries.AllIds(map));

		Assert.True(AreaPicker.TryPick(map, args, out var result));

		var relation = Assert.IsType<AreaRelation.TriangulatedLandmarks>(result.Relation);
		Assert.Equal(relation.LandmarkAId, result.Intel.LandmarkAId);
		Assert.Equal(relation.LandmarkBId, result.Intel.LandmarkBId);
		Assert.Equal(relation.LandmarkCId, result.Intel.LandmarkCId);
		Assert.True(map.PathfindingTerrain.IsCircleTraversable(result.Center, result.Radius));
	}

	[Fact]
	public void TryPick_PoiAndNavigationPool_Succeeds()
	{
		const int span = 200;
		var map = AreaPickerTestMaps.OpenPoiAndNavigationLandmark(span);
		var args = new AreaPickerArgs(MapLandmarkQueries.AllIds(map));

		Assert.True(AreaPicker.TryPick(map, args, out var result));
		Assert.IsType<AreaRelation.TriangulatedLandmarks>(result.Relation);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(7)]
	[InlineData(42)]
	[InlineData(99)]
	[InlineData(500)]
	public void OfferBeatA_SearchAreaUsesThreeLandmarks(int seed)
	{
		var map = maps.FreshWithBeatAHunt(seed);
		var hunt = Assert.IsType<HuntObjective>(map.ContractRegistry.Pending.Single().Objective);
		var searchArea = hunt.SpawnGroups[0].SearchArea;

		Assert.IsType<AreaRelation.TriangulatedLandmarks>(searchArea.Relation);
		Assert.True(map.PathfindingTerrain.IsCircleTraversable(searchArea.Center, searchArea.Radius));
		Assert.True(AreaIntelDisplay.TryParseLinkableSegments(searchArea.Intel, out _));
		AssertIntelLandmarksOrderedByDistance(map, searchArea);
	}

	private static void AssertIntelLandmarksOrderedByDistance(StarMap map, AreaPick pick)
	{
		var intel = pick.Intel;
		MapLandmarkQueries.TryGet(map, intel.LandmarkAId, out var a);
		MapLandmarkQueries.TryGet(map, intel.LandmarkBId, out var b);
		MapLandmarkQueries.TryGet(map, intel.LandmarkCId, out var c);
		var d0 = RouteGeometry.Distance(pick.Center, a.Position);
		var d1 = RouteGeometry.Distance(pick.Center, b.Position);
		var d2 = RouteGeometry.Distance(pick.Center, c.Position);
		Assert.True(d0 <= d1 && d1 <= d2);
	}
}
