using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

[StarSystemTestSuite]
public sealed class PilotRecruitmentRollTests
{
	[Fact]
	public void RollCandidates_IsDeterministicForSameMapSeedAndContract()
	{
		var map = StarMap.Create(42);
		const string contractId = "lead-contract-1";

		var first = PilotRecruitment.RollCandidates(map, contractId);
		var second = PilotRecruitment.RollCandidates(map, contractId);

		Assert.Equal(first.Count, second.Count);
		for (var index = 0; index < first.Count; index++)
		{
			Assert.Equal(first[index].Slot, second[index].Slot);
			Assert.Equal(first[index].Profile.Name, second[index].Profile.Name);
			Assert.Equal(first[index].Profile.PortraitId, second[index].Profile.PortraitId);
			Assert.Equal(first[index].Profile.BackgroundId, second[index].Profile.BackgroundId);
			Assert.Equal(first[index].Profile.PositiveQuirkId, second[index].Profile.PositiveQuirkId);
			Assert.Equal(first[index].Profile.NegativeQuirkId, second[index].Profile.NegativeQuirkId);
			Assert.Equal(first[index].Ship.Chassis, second[index].Ship.Chassis);
			Assert.Equal(first[index].Ship.GearTier, second[index].Ship.GearTier);
			Assert.Equal(first[index].Ship.ShipId, second[index].Ship.ShipId);
		}
	}

	[Fact]
	public void RollCandidates_BundlesRepurposedMinerForEverySlot()
	{
		var map = StarMap.Create(7);
		var candidates = PilotRecruitment.RollCandidates(map, "lead-contract-2");

		Assert.Equal(PilotRecruitment.CandidatesPerLead, candidates.Count);
		Assert.All(
			candidates,
			candidate =>
			{
				Assert.Equal(PilotRecruitment.DefaultHireChassis, candidate.Ship.Chassis);
				Assert.Equal(PilotRecruitment.DefaultHireGearTier, candidate.Ship.GearTier);
			});
	}

	[Fact]
	public void RollCandidates_OmitsQuirksWhilePoolsAreEmpty()
	{
		var map = StarMap.Create(11);
		var candidates = PilotRecruitment.RollCandidates(map, "lead-contract-3");

		foreach (var candidate in candidates)
		{
			Assert.Null(candidate.Profile.PositiveQuirkId);
			Assert.Null(candidate.Profile.NegativeQuirkId);
		}
	}

	[Fact]
	public void RollCandidates_StartsAtLevelOneWithZeroExperience()
	{
		var map = StarMap.Create(13);
		var candidate = Assert.Single(
			PilotRecruitment.RollCandidates(map, "lead-contract-4"),
			row => row.Slot == 0);

		Assert.Equal(0, candidate.Profile.ExperiencePoints);
		Assert.Equal(PilotProgression.LevelFor(0), candidate.Profile.ClaimedLevel);
		Assert.Equal(1, candidate.Profile.ClaimedLevel);
	}

	[Fact]
	public void RollCandidates_ThrowsWhenNoBackgroundsUnlockedAtTier()
	{
		var map = StarMap.Create(42);
		MerchantCatalogTestBands.SetBand(map, EMerchantAvailabilityBand.T1);

		var error = Assert.Throws<InvalidOperationException>(() =>
			PilotRecruitment.RollCandidates(map, "lead-contract-5"));
		Assert.Contains("backgrounds", error.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void DefaultHirePrice_UsesMarketListingWhenAvailable()
	{
		var map = PilotRecruitmentRollTestHarness.CreateMapWithRepurposedMinerListing(42);

		Assert.Equal(
			ResourceBundle.Of(ResourceId.Credits, 300),
			PilotRecruitment.DefaultHirePrice(map));
	}

	[Fact]
	public void DefaultHirePrice_FallsBackToSeedCostWithoutListing()
	{
		var map = StarMap.Create(42);

		Assert.Equal(
			ResourceBundle.Of(ResourceId.Credits, StarMarketSeed.RepurposedMinerCreditCost),
			PilotRecruitment.DefaultHirePrice(map));
	}

	[Fact]
	public void RollAvailableCandidates_ExcludesPurchasedSlots()
	{
		var map = StarMap.Create(17);
		var all = PilotRecruitment.RollCandidates(map, "lead-contract-7");
		var available = PilotRecruitment.RollAvailableCandidates(
			map,
			"lead-contract-7",
			[1]);

		Assert.Equal(PilotRecruitment.CandidatesPerLead, all.Count);
		Assert.Equal(2, available.Count);
		Assert.DoesNotContain(available, candidate => candidate.Slot == 1);
	}

	[Fact]
	public void ListUnlockedBackgrounds_AtT1_IsEmptyWhileMinimumBandsAreT0()
	{
		Assert.Empty(PilotRecruitment.ListUnlockedBackgrounds(EMerchantAvailabilityBand.T1));
	}
}
