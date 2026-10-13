using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

[StarSystemTestSuite]
public sealed class StarMarketTests
{
	[Fact]
	public void Seed_IncludesTieredUpgradesHealingAndRecruitment()
	{
		var market = StarMarketSeed.Create();

		Assert.Contains(
			market.MerchantListings,
			listing => listing.Id == "upgrade.weapons.damage.1");
		Assert.Contains(
			market.MerchantListings,
			listing => listing.Id == "healing.repair_hull");
		Assert.Contains(
			market.MerchantListings,
			listing =>
				listing.Pricing.Kind == MerchantListingPricingKind.ShieldRechargePerPoint);
		Assert.Equal(2, market.RecruitmentListings.Count);
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
			candidate.Id == "upgrade.weapons.damage.1");

		listing.SetPricing(MerchantListingPricing.Fixed(
			ResourceBundle.Of(ResourceId.Credits, 1)));

		var original = map.Market.MerchantListings.First(candidate =>
			candidate.Id == "upgrade.weapons.damage.1");
		Assert.NotEqual(
			Credits(original.Pricing.FixedCost),
			Credits(listing.Pricing.FixedCost));
	}

	[Fact]
	public void BandFiltering_MatchesMerchantCatalogIncludesSheet()
	{
		var market = StarMarketSeed.Create();
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);

		var t0Offers = MerchantCatalog.ListFor(
			EMerchantCatalog.ShipSupport,
			ship,
			market,
			EMerchantAvailabilityBand.T0);
		Assert.Single(t0Offers);

		var t1Offers = MerchantCatalog.ListFor(
			EMerchantCatalog.ShipSupport,
			ship,
			market,
			EMerchantAvailabilityBand.T1);
		Assert.DoesNotContain(
			t1Offers,
			offer => offer.Offering.TargetLevel == 1);
	}

	private static int Credits(ResourceBundle bundle)
	{
		Assert.True(bundle.TryGet(ResourceId.Credits, out var amount));
		return amount;
	}
}
