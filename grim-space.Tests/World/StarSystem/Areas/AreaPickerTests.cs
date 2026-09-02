using GrimSpace.Math.Routes;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Generation;

namespace GrimSpace.Tests.World.StarSystem.Areas;

public sealed class AreaPickerTests
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
		var map = StarMap.CreateDevDefault(0);
		Assert.Throws<ArgumentNullException>(() =>
			AreaPicker.Pick(map, null!, [EAreaDistance.Low], 2));
	}

	[Fact]
	public void Pick_NullDistances_Throws()
	{
		var map = StarMap.CreateDevDefault(0);
		Assert.Throws<ArgumentNullException>(() =>
			AreaPicker.Pick(map, [["poi-refinery", "poi-storage"]], null!, 2));
	}

	[Fact]
	public void Pick_InvalidLandmarksToPick_Throws()
	{
		var map = StarMap.CreateDevDefault(0);
		Assert.Throws<ArgumentException>(() =>
			AreaPicker.Pick(map, [["poi-refinery", "poi-storage"]], [EAreaDistance.Low], 0));
	}

	[Fact]
	public void Pick_UnsupportedLandmarksToPick_Throws()
	{
		var map = StarMap.CreateDevDefault(0);
		Assert.Throws<ArgumentException>(() =>
			AreaPicker.Pick(map, [["poi-refinery", "poi-storage"]], [EAreaDistance.Low], 1));
	}

	[Fact]
	public void Pick_EmptyGroups_Throws()
	{
		var map = StarMap.CreateDevDefault(0);
		Assert.Throws<ArgumentException>(() =>
			AreaPicker.Pick(map, [], [EAreaDistance.Low], 2));
	}

	[Fact]
	public void Pick_EmptyDistances_Throws()
	{
		var map = StarMap.CreateDevDefault(0);
		Assert.Throws<ArgumentException>(() =>
			AreaPicker.Pick(map, [["poi-refinery", "poi-storage"]], [], 2));
	}

	[Fact]
	public void Pick_EmptyInnerGroup_Throws()
	{
		var map = StarMap.CreateDevDefault(0);
		Assert.Throws<ArgumentException>(() =>
			AreaPicker.Pick(map, [[]], [EAreaDistance.Low], 2));
	}

	[Fact]
	public void Pick_UndersizedGroup_Throws()
	{
		var map = StarMap.CreateDevDefault(0);
		var plan = map.Blueprint.SupplyPlan;
		Assert.Throws<ArgumentException>(() =>
			AreaPicker.Pick(map, [[plan.RefineryPoiId]], [EAreaDistance.Low], 2));
	}

	[Fact]
	public void Pick_UnknownLandmark_Throws()
	{
		var map = StarMap.CreateDevDefault(0);
		Assert.Throws<ArgumentException>(() =>
			AreaPicker.Pick(map, [["poi-missing", "poi-storage"]], [EAreaDistance.Low], 2));
	}

	[Fact]
	public void Pick_UnroutedLandmarkPair_StillPlacesRelativeToLandmarkAxis()
	{
		var map = StarMap.CreateDevDefault(42);
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
		var map = StarMap.CreateDevDefault(42);
		var plan = map.Blueprint.SupplyPlan;
		var group = new[] { plan.RefineryPoiId, plan.StoragePoiId };

		var result = AreaPicker.Pick(map, [group], [EAreaDistance.Low], 2);

		var relation = Assert.IsType<AreaRelation.BetweenLandmarks>(result.Relation);
		Assert.Equal(plan.RefineryPoiId, relation.LandmarkAId);
		Assert.Equal(plan.StoragePoiId, relation.LandmarkBId);
		Assert.Contains(relation.LandmarkAId, group);
		Assert.Contains(relation.LandmarkBId, group);
	}

	[Fact]
	public void Pick_MalformedGroupInCollection_ThrowsBeforeSampling()
	{
		var map = StarMap.CreateDevDefault(0);
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
	public void Pick_Suitability_MeetsBandCriteria(EAreaDistance distance)
	{
		var seeds = new[] { 0, 1, 7, 42, 99, 123, 500 };
		var distanceConfig = new AreaDistanceConfig();

		foreach (var seed in seeds)
		{
			var map = StarMap.CreateDevDefault(seed);
			var plan = map.Blueprint.SupplyPlan;
			var group = new[] { plan.RefineryPoiId, plan.StoragePoiId };
			var result = AreaPicker.Pick(map, [group], [distance], 2, distanceConfig);

			AssertBandCriteria(map, result, distance, distanceConfig);
			AssertIntel(map, result);
			AssertRadiusScalesWithSpan(map, result);
		}
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

		Assert.Contains(displayA, pick.Description);
		Assert.Contains(displayB, pick.Description);
		Assert.False(string.IsNullOrWhiteSpace(pick.Description));
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
