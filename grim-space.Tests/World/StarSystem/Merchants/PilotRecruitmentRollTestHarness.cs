using GrimSpace.Run.Items;
using GrimSpace.Run.Persistence;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

internal static class PilotRecruitmentRollTestHarness
{
	internal static StarMap CreateMapWithRepurposedMinerListing(
		int seed = 42,
		Inventory? itemInventory = null)
	{
		var map = StarMap.Create(seed, itemInventory);
		var registry = PersistenceRegistry.CreateDefault();
		var captured = SaveDtoMapper.CaptureStarMap(map, registry);
		var recruitment = captured.Market!.RecruitmentListings.ToList();
		recruitment.Add(
			new RecruitmentListingDto(
				"test.recruitment.miner",
				EType.RepurposedMiner,
				EShipGearTier.T0,
				EMerchantAvailabilityBand.T0,
				ResourceBundle.Of(ResourceId.Credits, 300)));

		return SaveDtoMapper.RestoreStarMap(
			captured with { Market = captured.Market with { RecruitmentListings = recruitment } },
			registry,
			itemInventory);
	}

}
