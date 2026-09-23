using GrimSpace.Math.Grid;
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
	public void TryPick_LandmarkWithBorderTriangle_OnNavigationTriangle_Succeeds()
	{
		var map = AreaPickerTestMaps.OpenNavigationTriangle(240);
		const double borderFraction = 0.5;
		var args = new AreaPickerArgs(
			MapLandmarkQueries.AllIds(map),
			ReferenceMode: EAreaPickerReferenceMode.LandmarkWithBorderTriangle,
			BorderReferenceConfig: new AreaBorderReferenceConfig(borderFraction),
			DeterministicPickMix: 3);

		Assert.True(AreaPicker.TryPick(map, args, out var result));
		var relation = Assert.IsType<AreaRelation.LandmarkWithBorderTriangle>(result.Relation);
		MapLandmarkQueries.TryGet(map, relation.LandmarkId, out var landmark);
		var maximumDistance = borderFraction * RouteGeometry.Distance(
			new Coord(0, 0, 0),
			new Coord(map.Width - 1, 0, map.Height - 1));
		var distanceFromBorder = System.Math.Min(
			System.Math.Min(landmark.Position.X, map.Width - 1 - landmark.Position.X),
			System.Math.Min(landmark.Position.Z, map.Height - 1 - landmark.Position.Z));
		Assert.True(distanceFromBorder <= maximumDistance);
		Assert.True(
			(relation.BorderPointA.Z == relation.BorderPointB.Z
				&& (relation.BorderPointA.Z == 0 || relation.BorderPointA.Z == map.Height - 1))
			|| (relation.BorderPointA.X == relation.BorderPointB.X
				&& (relation.BorderPointA.X == 0 || relation.BorderPointA.X == map.Width - 1)));
		Assert.True(map.PathfindingTerrain.IsCircleTraversable(result.Center, result.Radius));
	}

	[Fact]
	public void TryPick_LandmarkWithBorderTriangle_DeterministicMixCanVaryAnchorLandmark()
	{
		var map = AreaPickerTestMaps.OpenNavigationTriangle(240);
		var args = new AreaPickerArgs(
			MapLandmarkQueries.AllIds(map),
			ReferenceMode: EAreaPickerReferenceMode.LandmarkWithBorderTriangle,
			BorderReferenceConfig: new AreaBorderReferenceConfig(0.5));

		var landmarkIds = new HashSet<string>(StringComparer.Ordinal);
		for (var mix = 0; mix < 32; mix++)
		{
			var tryArgs = args with { DeterministicPickMix = mix };
			if (!AreaPicker.TryPick(map, tryArgs, out var result))
				continue;

			var relation = Assert.IsType<AreaRelation.LandmarkWithBorderTriangle>(result.Relation);
			landmarkIds.Add(relation.LandmarkId);
		}

		Assert.True(landmarkIds.Count >= 2, $"Expected multiple anchor landmarks, got: {string.Join(", ", landmarkIds)}");
	}

	[Fact]
	public void TryPick_LandmarkWithBorderTriangle_UsesConfiguredLandmark()
	{
		var map = AreaPickerTestMaps.OpenSingleCenterLandmark(512);
		var args = new AreaPickerArgs(
			[AreaPickerTestMaps.LandmarkAId],
			ReferenceMode: EAreaPickerReferenceMode.LandmarkWithBorderTriangle,
			BorderReferenceConfig: new AreaBorderReferenceConfig(0.5),
			DeterministicPickMix: 7);

		Assert.True(AreaPicker.TryPick(map, args, out var result));

		var relation = Assert.IsType<AreaRelation.LandmarkWithBorderTriangle>(result.Relation);
		Assert.Equal(AreaPickerTestMaps.LandmarkAId, relation.LandmarkId);
		Assert.True(map.PathfindingTerrain.IsCircleTraversable(result.Center, result.Radius));
		Assert.True(AreaIntelDisplay.TryParseLinkableSegments(result.Intel, out _));
	}

	[Fact]
	public void TryPick_LandmarkWithBorderTriangle_ReturnsFalseWithNoCandidates()
	{
		var map = AreaPickerTestMaps.OpenSingleCenterLandmark();
		var args = new AreaPickerArgs(
			[],
			ReferenceMode: EAreaPickerReferenceMode.LandmarkWithBorderTriangle);

		Assert.False(AreaPicker.TryPick(map, args, out _));
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
		Assert.Equal(
			new HashSet<string> { relation.LandmarkAId, relation.LandmarkBId, relation.LandmarkCId },
			new HashSet<string>
			{
				result.Intel.LandmarkAId,
				result.Intel.LandmarkBId,
				result.Intel.LandmarkCId,
			});
		AssertIntelLandmarksOrderedByDistance(map, result);
		Assert.True(AreaIntelDisplay.TryParseLinkableSegments(result.Intel, out _));
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
