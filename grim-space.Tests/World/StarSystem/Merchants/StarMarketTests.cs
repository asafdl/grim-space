using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

[StarSystemTestSuite]
public sealed class StarMarketTests
{
	[Fact]
	public void Seed_IncludesHealingAndTieredFighterRecruitment()
	{
		var market = StarMarketSeed.Create();

		Assert.Contains(market.MerchantListings, listing => listing.Id == "healing.repair_hull");
		Assert.Contains(
			market.MerchantListings,
					listing => listing.Pricing.Kind == MerchantListingPricingKind.ShieldRechargePerPoint);
		Assert.Equal(3, market.RecruitmentListings.Count);
		Assert.All(market.RecruitmentListings, listing => Assert.Equal(EType.Fighter, listing.Chassis));
	}

	[Fact]
	public void GeneratedMap_HasOneGlobalMarket()
	{
		var map = StarMap.Create(42);

		Assert.NotNull(map.Market);
		Assert.True(map.Market.MerchantListings.Count > 0);
	}

	[Fact]
	public void Fork_IsolatesListingCurrentPrices()
	{
		var map = StarMap.Create(42);
		var fork = map.Fork();
		var listing = fork.Market.MerchantListings.First(candidate =>
			candidate.Id == "healing.repair_hull");

		listing.SetPricing(MerchantListingPricing.Fixed(
			ResourceBundle.Of(ResourceId.Credits, 1)));

		var original = map.Market.MerchantListings.First(candidate =>
			candidate.Id == "healing.repair_hull");
		Assert.NotEqual(
			Credits(original.Pricing.FixedCost),
			Credits(listing.Pricing.FixedCost));
	}

	[Theory]
	[InlineData(EMerchantAvailabilityBand.T0, EShipGearTier.T0, StarMarketSeed.FighterT0CreditCost)]
	[InlineData(EMerchantAvailabilityBand.T1, EShipGearTier.T1, StarMarketSeed.FighterT1CreditCost)]
	[InlineData(EMerchantAvailabilityBand.T2, EShipGearTier.T2, StarMarketSeed.FighterT2CreditCost)]
	public void Broker_OffersOnlyBandFighterWithTieredPrice(
		EMerchantAvailabilityBand band,
		EShipGearTier expectedTier,
		int expectedCredits)
	{
		var offers = StarMarketSeed.Create().ListRecruitmentOffers(band);
		var offer = Assert.Single(offers);

		Assert.Equal(EType.Fighter, offer.Chassis);
		Assert.Equal(expectedTier, offer.GearTier);
		Assert.Equal(expectedCredits, Credits(offer.Cost));
		if (band == EMerchantAvailabilityBand.T2)
			Assert.Equal(StarMarketSeed.FighterT2IndustrialCoreCost, Amount(offer.Cost, ResourceId.IndustrialCore));
		else
			Assert.False(offer.Cost.TryGet(ResourceId.IndustrialCore, out _));
		Assert.All(offers, candidate => Assert.Equal(EType.Fighter, candidate.Chassis));
	}

	[Theory]
	[InlineData(EShipGearTier.T1)]
	[InlineData(EShipGearTier.T2)]
	public void PurchasedFighterTier_HasStrongerLoadoutThanT0(EShipGearTier tier)
	{
		var t0 = ShipCatalog.LoadoutForTier(EType.Fighter, EShipGearTier.T0, rollSeed: 17);
		var purchased = ShipCatalog.LoadoutForTier(EType.Fighter, tier, rollSeed: 17);

		Assert.True(LoadoutStrength(purchased) > LoadoutStrength(t0));
	}

	private static int LoadoutStrength(ShipLoadout loadout) =>
		loadout.MaxHullPoints
		+ Enum.GetValues<ESpatialOrientation>().Sum(face => loadout.MaxShieldPoints[face])
		+ loadout.InstalledAbilities.Sum(ability =>
			ability.Spec.DamageUpgradeTier + ability.Spec.RangeUpgradeTier);

	private static int Credits(ResourceBundle bundle) => Amount(bundle, ResourceId.Credits);

	private static int Amount(ResourceBundle bundle, ResourceId resource)
	{
		Assert.True(bundle.TryGet(resource, out var amount));
		return amount;
	}
}
