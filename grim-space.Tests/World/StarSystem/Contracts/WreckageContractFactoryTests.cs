using GrimSpace.Math;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Generation;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Landmarks;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.Tests.World.StarSystem;
using GrimSpace.Tests.World.StarSystem.Areas;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class WreckageContractFactoryTests(StarMapFixture maps)
{
	[Fact]
	public void TryBuildWreckage_SucceedsOnDefaultSupplyMap()
	{
		var map = maps.Fresh(42);
		var args = CreateWreckageArgs(map, tick: 3, slot: 1);

		Assert.True(ContractFactory.TryBuildWreckage(map, "contract-wreckage-gen", args, out var contract));
		Assert.IsType<WreckageObjective>(contract.Objective);
		var wreckage = (WreckageObjective)contract.Objective;
		Assert.Single(wreckage.SearchArea.SpawnPoints);
		Assert.True(AreaIntelDisplay.TryParseLinkableSegments(wreckage.SearchArea.Intel, out _));
	}

	[Fact]
	public void TryBuildWreckage_FailsWhenNoNavigationLandmarks()
	{
		var map = AreaPickerTestMaps.OpenSingleCenterLandmark();
		var args = CreateWreckageArgs(map, tick: 1, slot: 0);

		Assert.False(ContractFactory.TryBuildWreckage(map, "contract-wreckage-miss", args, out _));
	}

	[Fact]
	public void TryBuildWreckage_IntelReferencesOnlyNavLandmarksOrBorderAnchors()
	{
		var map = maps.Fresh(77);
		var navIds = map.NavigationLandmarks
			.Select(landmark => landmark.Id)
			.ToHashSet(StringComparer.Ordinal);
		var args = CreateWreckageArgs(map, tick: 8, slot: 2);

		Assert.True(ContractFactory.TryBuildWreckage(map, "contract-wreckage-intel", args, out var contract));
		var intel = ((WreckageObjective)contract.Objective).SearchArea.Intel;
		foreach (var id in new[] { intel.LandmarkAId, intel.LandmarkBId, intel.LandmarkCId })
			Assert.True(navIds.Contains(id) || AreaBorderAnchor.TryParseId(id, out _));
	}

	[Fact]
	public void TryBuildWreckage_OpenNavigationTriangle_SucceedsWithBorderOrTriMode()
	{
		var map = AreaPickerTestMaps.OpenNavigationTriangle(240);
		var args = CreateWreckageArgs(
			map,
			tick: 4,
			slot: 0,
			minimumPoiClearance: 0,
			EAreaPickerReferenceMode.LandmarkWithBorderTriangle);

		Assert.True(ContractFactory.TryBuildWreckage(map, "contract-wreckage-triangle", args, out _));
	}

	public static WreckageCreateArgs CreateWreckageArgs(
		StarMap map,
		int tick,
		int slot,
		int minimumPoiClearance = 16,
		EAreaPickerReferenceMode? referenceMode = null)
	{
		var navLandmarkIds = map.NavigationLandmarks
			.Select(landmark => landmark.Id)
			.OrderBy(id => id, StringComparer.Ordinal)
			.ToArray();
		var areaPickMix = (long)StableSeedMixer.From(map.Seed).Add(tick).Add(slot).Add("wreckage-area").Value;
		var modeRandom = new StableRandom(
			StableSeedMixer.From(map.Seed).Add(tick).Add(slot).Add("wreckage-area-mode").Value);
		var mode = referenceMode
			?? (modeRandom.NextDouble() < 0.75f
				? EAreaPickerReferenceMode.LandmarkWithBorderTriangle
				: EAreaPickerReferenceMode.TriangulateLandmarks);

		return new WreckageCreateArgs(
			ContractActionTestContext.AdministrativePoiId,
			new AreaPickerArgs(
				navLandmarkIds,
				minimumPoiClearance,
				DeterministicPickMix: areaPickMix,
				ReferenceMode: mode,
				BorderReferenceConfig: new AreaBorderReferenceConfig()),
			new WreckageOutcome.Salvage(ResourceBundle.Of(ResourceId.ScrapAlloy, 2)),
			new ContractTerms(ResourceBundle.Of(ResourceId.Credits, 60)),
			new ContractNarrative("Test Wreck", "Briefing."),
			IsStoryObjective: false);
	}
}
