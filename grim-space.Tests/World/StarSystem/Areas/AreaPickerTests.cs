using GrimSpace.Math.Routes;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Areas;

public sealed class AreaPickerTests(StarMapFixture maps)
{
	[Fact]
	public void Pick_NullMap_Throws()
	{
		Assert.Throws<ArgumentNullException>(() =>
			AreaPicker.Pick(null!, [["poi-refinery", "poi-storage"]], [EAreaDistance.Low], 2));
	}

	[Fact]
	public void Pick_NullGroups_Throws()
	{
		var map = maps.Template(0);
		Assert.Throws<ArgumentNullException>(() =>
			AreaPicker.Pick(map, null!, [EAreaDistance.Low], 2));
	}

	[Fact]
	public void Pick_NullDistances_Throws()
	{
		var map = maps.Template(0);
		Assert.Throws<ArgumentNullException>(() =>
			AreaPicker.Pick(map, [["poi-refinery", "poi-storage"]], null!, 2));
	}

	[Fact]
	public void Pick_InvalidLandmarksToPick_Throws()
	{
		var map = maps.Template(0);
		Assert.Throws<ArgumentException>(() =>
			AreaPicker.Pick(map, [["poi-refinery", "poi-storage"]], [EAreaDistance.Low], 0));
	}

	[Fact]
	public void Pick_UnsupportedLandmarksToPick_Throws()
	{
		var map = maps.Template(0);
		Assert.Throws<ArgumentException>(() =>
			AreaPicker.Pick(map, [["poi-refinery", "poi-storage"]], [EAreaDistance.Low], 1));
	}

	[Fact]
	public void Pick_EmptyGroups_Throws()
	{
		var map = maps.Template(0);
		Assert.Throws<ArgumentException>(() =>
			AreaPicker.Pick(map, [], [EAreaDistance.Low], 2));
	}

	[Fact]
	public void Pick_EmptyDistances_Throws()
	{
		var map = maps.Template(0);
		Assert.Throws<ArgumentException>(() =>
			AreaPicker.Pick(map, [["poi-refinery", "poi-storage"]], [], 2));
	}

	[Fact]
	public void Pick_EmptyInnerGroup_Throws()
	{
		var map = maps.Template(0);
		Assert.Throws<ArgumentException>(() =>
			AreaPicker.Pick(map, [[]], [EAreaDistance.Low], 2));
	}

	[Fact]
	public void Pick_UndersizedGroup_Throws()
	{
		var map = maps.Template(0);
		var plan = map.Blueprint.SupplyPlan;
		Assert.Throws<ArgumentException>(() =>
			AreaPicker.Pick(map, [[plan.RefineryPoiId]], [EAreaDistance.Low], 2));
	}

	[Fact]
	public void Pick_UnknownLandmark_Throws()
	{
		var map = maps.Template(0);
		Assert.Throws<ArgumentException>(() =>
			AreaPicker.Pick(map, [["poi-missing", "poi-storage"]], [EAreaDistance.Low], 2));
	}

	[Fact]
	public void Pick_UnroutedLandmarkPair_StillPlacesRelativeToLandmarkAxis()
	{
		var map = maps.Template(42);
		var plan = map.Blueprint.SupplyPlan;
		var result = AreaPicker.Pick(
			map,
			[[plan.ExtractionPoiId, plan.StoragePoiId]],
			[EAreaDistance.Low],
			2);

		var relation = Assert.IsType<AreaRelation.BetweenLandmarks>(result.Relation);
		Assert.Equal(plan.ExtractionPoiId, relation.LandmarkAId);
		Assert.Equal(plan.StoragePoiId, relation.LandmarkBId);
	}

	[Fact]
	public void Pick_ExplicitPairGroup_ReturnsValidLandmarks()
	{
		var map = maps.Template(42);
		var plan = map.Blueprint.SupplyPlan;
		var group = new[] { plan.ExtractionPoiId, plan.StoragePoiId };

		var result = AreaPicker.Pick(map, [group], [EAreaDistance.High], 2);

		var relation = Assert.IsType<AreaRelation.BetweenLandmarks>(result.Relation);
		Assert.Equal(plan.ExtractionPoiId, relation.LandmarkAId);
		Assert.Equal(plan.StoragePoiId, relation.LandmarkBId);
		Assert.Contains(relation.LandmarkAId, group);
		Assert.Contains(relation.LandmarkBId, group);
	}

	[Fact]
	public void Pick_MalformedGroupInCollection_ThrowsBeforeSampling()
	{
		var map = maps.Template(0);
		var plan = map.Blueprint.SupplyPlan;
		Assert.Throws<ArgumentException>(() =>
			AreaPicker.Pick(
				map,
				[[plan.RefineryPoiId, plan.StoragePoiId], []],
				[EAreaDistance.Low],
				2));
	}

	[Theory]
	[InlineData(EAreaDistance.Low)]
	[InlineData(EAreaDistance.Med)]
	[InlineData(EAreaDistance.High)]
	public void Pick_OnOpenTerrain_MeetsBandCriteria(EAreaDistance distance)
	{
		const int span = 200;
		var map = AreaPickerTestMaps.OpenLandmarkPair(span);
		var group = new[] { AreaPickerTestMaps.LandmarkAId, AreaPickerTestMaps.LandmarkBId };
		var distanceConfig = new AreaDistanceConfig();
		var result = AreaPicker.Pick(map, [group], [distance], 2, distanceConfig);

		AssertBandCriteria(map, result, distance, distanceConfig);
		Assert.True(map.PathfindingTerrain.IsCircleTraversable(result.Center, result.Radius));
		AssertIntel(map, result);
		AssertRadiusScalesWithSpan(map, result);
	}

	[Theory]
	[InlineData(EAreaDistance.Low)]
	[InlineData(EAreaDistance.Med)]
	public void Pick_ThrowsWhenBandGeometryIsImpossible(EAreaDistance distance)
	{
		const int span = 100;
		var map = AreaPickerTestMaps.OpenLandmarkPair(span);
		var group = new[] { AreaPickerTestMaps.LandmarkAId, AreaPickerTestMaps.LandmarkBId };

		Assert.Throws<InvalidOperationException>(() =>
			AreaPicker.Pick(map, [group], [distance], 2));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(7)]
	[InlineData(42)]
	[InlineData(99)]
	[InlineData(500)]
	public void Create_SeedsStarterSearchAreaThatMeetsBandCriteria(int seed)
	{
		var map = maps.Template(seed);
		var hunt = Assert.IsType<HuntObjective>(map.ContractRegistry.Offered.Single().Objective);
		var searchArea = hunt.SpawnGroups[0].SearchArea;
		var relation = Assert.IsType<AreaRelation.BetweenLandmarks>(searchArea.Relation);
		var distanceConfig = new AreaDistanceConfig();

		AssertBandCriteria(map, searchArea, relation.Distance, distanceConfig);
		Assert.True(map.PathfindingTerrain.IsCircleTraversable(searchArea.Center, searchArea.Radius));
		AssertIntel(map, searchArea);
		AssertRadiusScalesWithSpan(map, searchArea);
	}

	private static void AssertBandCriteria(
		StarMap map,
		AreaPick pick,
		EAreaDistance distance,
		AreaDistanceConfig config)
	{
		var relation = Assert.IsType<AreaRelation.BetweenLandmarks>(pick.Relation);
		var poiA = map.PointsOfInterest.First(poi => poi.Id == relation.LandmarkAId);
		var poiB = map.PointsOfInterest.First(poi => poi.Id == relation.LandmarkBId);
		var span = RouteGeometry.Distance(poiA.PlacedCenter, poiB.PlacedCenter);
		var axis = new[] { poiA.PlacedCenter, poiB.PlacedCenter };
		var axisDistance = RouteGeometry.PointToPolylineDistance(pick.Center, axis);

		switch (distance)
		{
			case EAreaDistance.Low:
				Assert.True(axisDistance + pick.Radius <= span * config.LowFraction);
				break;
			case EAreaDistance.Med:
				Assert.True(axisDistance - pick.Radius >= span * config.MedMinFraction);
				Assert.True(axisDistance + pick.Radius <= span * config.MedMaxFraction);
				break;
			case EAreaDistance.High:
				Assert.True(axisDistance - pick.Radius >= span * config.HighMinFraction);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(distance), distance, null);
		}
	}

	private static void AssertIntel(StarMap map, AreaPick pick)
	{
		var relation = Assert.IsType<AreaRelation.BetweenLandmarks>(pick.Relation);
		var displayA = map.PointsOfInterest.First(poi => poi.Id == relation.LandmarkAId).DisplayName;
		var displayB = map.PointsOfInterest.First(poi => poi.Id == relation.LandmarkBId).DisplayName;
		var description = AreaIntelDisplay.FormatPlain(
			pick.Intel,
			poiId => map.PointsOfInterest.FirstOrDefault(poi => poi.Id == poiId)?.DisplayName);

		Assert.Equal(relation.LandmarkAId, pick.Intel.LandmarkAId);
		Assert.Equal(relation.LandmarkBId, pick.Intel.LandmarkBId);
		Assert.Contains(displayA, description);
		Assert.Contains(displayB, description);
		Assert.False(string.IsNullOrWhiteSpace(description));
	}

	private static void AssertRadiusScalesWithSpan(StarMap map, AreaPick pick)
	{
		var relation = Assert.IsType<AreaRelation.BetweenLandmarks>(pick.Relation);
		var poiA = map.PointsOfInterest.First(poi => poi.Id == relation.LandmarkAId);
		var poiB = map.PointsOfInterest.First(poi => poi.Id == relation.LandmarkBId);
		var span = RouteGeometry.Distance(poiA.PlacedCenter, poiB.PlacedCenter);
		var expected = AreaRadiusPicker.Pick(span);

		Assert.Equal(expected, pick.Radius);
	}
}
