using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Economy;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Merchants;

public static class StarMarketSeed
{
	public const int HullRepairCreditCost = 10;
	public const int HullRepairScrapCost = 10;
	public const int ShieldRechargeCreditsPerPoint = 5;
	public const int PilotHireCreditCost = 75;
	public const int FighterT0CreditCost = 150;
	public const int FighterT1CreditCost = 300;
	public const int FighterT2CreditCost = 600;
	public const int FighterT2IndustrialCoreCost = 1;
	public const int FuelPurchaseCreditCost = StrategicEconomyConfig.FuelPurchaseCreditCost;
	public const int FuelPurchaseAmount = StrategicEconomyConfig.FuelPurchaseAmount;

	public static StarMarket Create()
	{
		var merchantListings = new List<MerchantListing>();
		merchantListings.Add(new MerchantListing(
			"healing.repair_hull",
			EMerchantCatalog.Healing,
			new MerchantCatalog.Offering(MerchantCatalog.Kind.RepairHull),
			EMerchantAvailabilityBand.T0,
			alwaysAvailable: true,
			MerchantListingPricing.Fixed(ResourceBundle.Create(
				(ResourceId.Credits, HullRepairCreditCost),
				(ResourceId.ScrapAlloy, HullRepairScrapCost)))));

		merchantListings.Add(new MerchantListing(
			"healing.shield_recharge_per_point",
			EMerchantCatalog.Healing,
			offering: null,
			EMerchantAvailabilityBand.T0,
			alwaysAvailable: true,
			MerchantListingPricing.ShieldRecharge(ShieldRechargeCreditsPerPoint)));

		merchantListings.Add(new MerchantListing(
			"healing.buy_fuel",
			EMerchantCatalog.Healing,
			new MerchantCatalog.Offering(MerchantCatalog.Kind.BuyFuel),
			EMerchantAvailabilityBand.T0,
			alwaysAvailable: true,
			MerchantListingPricing.Fixed(ResourceBundle.Of(
				ResourceId.Credits,
				FuelPurchaseCreditCost))));

		var recruitmentListings = new List<RecruitmentListing>
		{
			new(
				"recruitment.fighter.t0",
				EType.Fighter,
				EShipGearTier.T0,
				EMerchantAvailabilityBand.T0,
				ResourceBundle.Of(ResourceId.Credits, FighterT0CreditCost)),
			new(
				"recruitment.fighter.t1",
				EType.Fighter,
				EShipGearTier.T1,
				EMerchantAvailabilityBand.T1,
				ResourceBundle.Of(ResourceId.Credits, FighterT1CreditCost)),
			new(
				"recruitment.fighter.t2",
				EType.Fighter,
				EShipGearTier.T2,
				EMerchantAvailabilityBand.T2,
				ResourceBundle.Create(
					(ResourceId.Credits, FighterT2CreditCost),
					(ResourceId.IndustrialCore, FighterT2IndustrialCoreCost))),
		};

		return new StarMarket(merchantListings, recruitmentListings);
	}
}
