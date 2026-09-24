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
		Assert.Single(result.SpawnPoints);
		Assert.Contains(
			MapLandmarkQueries.AllIds(map),
			id => id == result.Intel.LandmarkAId
				|| id == result.Intel.LandmarkBId
				|| id == result.Intel.LandmarkCId);
		AssertIntelLandmarksOrderedByDistance(map, result);
	}

	[Fact]
	public void TryPick_LandmarkWithBorderTriangle_DeterministicMixCanVaryIntelLandmarks()
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

			foreach (var id in new[] { result.Intel.LandmarkAId, result.Intel.LandmarkBId, result.Intel.LandmarkCId })
			{
				if (MapLandmarkQueries.TryGet(map, id, out _))
					landmarkIds.Add(id);
			}
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

		Assert.Contains(
			new[] { result.Intel.LandmarkAId, result.Intel.LandmarkBId, result.Intel.LandmarkCId },
			id => id == AreaPickerTestMaps.LandmarkAId);
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
	public void TryPick_OnOpenTriangle_SamplesSpawnInLandmarkTriangle()
	{
		var map = AreaPickerTestMaps.OpenNavigationTriangle(240);
		var args = new AreaPickerArgs(MapLandmarkQueries.AllIds(map));

		Assert.True(AreaPicker.TryPick(map, args, out var result));

		Assert.Single(result.SpawnPoints);
		AssertIntelLandmarksOrderedByDistance(map, result);
		Assert.True(AreaIntelDisplay.TryParseLinkableSegments(result.Intel, out _));
	}

	[Fact]
	public void TryPick_PoiAndNavigationPool_Succeeds()
	{
		const int span = 200;
		var map = AreaPickerTestMaps.OpenPoiAndNavigationLandmark(span);
		var args = new AreaPickerArgs(MapLandmarkQueries.AllIds(map));

		Assert.True(AreaPicker.TryPick(map, args, out _));
	}

	[Fact]
	public void TryPick_MultipleSpawnSeeds_ReturnsDeterministicSpawnPoints()
	{
		var map = AreaPickerTestMaps.OpenNavigationTriangle(200);
		var args = new AreaPickerArgs(MapLandmarkQueries.AllIds(map), DeterministicPickMix: 11);
		var seeds = new ulong[] { 7, 19, 23 };

		Assert.True(AreaPicker.TryPick(map, args, seeds, out var first));
		Assert.True(AreaPicker.TryPick(map, args, seeds, out var second));

		Assert.Equal(3, first.SpawnPoints.Count);
		Assert.Equal(
			first.SpawnPoints.Select(coord => (coord.X, coord.Z)).ToList(),
			second.SpawnPoints.Select(coord => (coord.X, coord.Z)).ToList());
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

		Assert.Single(searchArea.SpawnPoints);
		Assert.True(AreaIntelDisplay.TryParseLinkableSegments(searchArea.Intel, out _));
		AssertIntelLandmarksOrderedByDistance(map, searchArea);
	}

	private static void AssertIntelLandmarksOrderedByDistance(StarMap map, AreaPick pick)
	{
		var intel = pick.Intel;
		var anchor = pick.SpawnPoints[0];
		var d0 = RouteGeometry.Distance(anchor, ResolveIntelReferencePosition(map, intel.LandmarkAId));
		var d1 = RouteGeometry.Distance(anchor, ResolveIntelReferencePosition(map, intel.LandmarkBId));
		var d2 = RouteGeometry.Distance(anchor, ResolveIntelReferencePosition(map, intel.LandmarkCId));
		Assert.True(d0 <= d1 && d1 <= d2);
	}

	private static Coord ResolveIntelReferencePosition(StarMap map, string id)
	{
		if (MapLandmarkQueries.TryGet(map, id, out var landmark))
			return landmark.Position;
		if (AreaBorderAnchor.TryParseId(id, out var border))
			return border;
		throw new InvalidOperationException($"Unknown intel reference '{id}'.");
	}
}
