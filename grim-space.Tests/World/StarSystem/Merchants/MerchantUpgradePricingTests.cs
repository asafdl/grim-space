using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

public sealed class MerchantUpgradePricingTests
{
	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	public void SeededUpgradeListings_IncreaseWithTargetTier(int targetTier)
	{
		var market = StarMarketSeed.Create();
		var damage = Credits(CostFor(market, MerchantCatalog.Kind.UpgradeDamage, targetTier));
		var range = Credits(CostFor(market, MerchantCatalog.Kind.UpgradeRange, targetTier));
		var shield = Credits(CostFor(market, MerchantCatalog.Kind.UpgradeMaxShields, targetTier));

		Assert.True(damage > range);
		Assert.True(range > shield);
	}

	[Fact]
	public void Tier3_DamagePackCostsMoreThanShieldPack()
	{
		var market = StarMarketSeed.Create();
		var damage = Credits(CostFor(market, MerchantCatalog.Kind.UpgradeDamage, 3));
		var shield = Credits(CostFor(market, MerchantCatalog.Kind.UpgradeMaxShields, 3));

		Assert.True(damage > shield);
	}

	private static ResourceBundle CostFor(
		StarMarket market,
		MerchantCatalog.Kind kind,
		int targetLevel)
	{
		var listing = market.MerchantListings.Single(candidate =>
			candidate.Offering is { } offering
			&& offering.Kind == kind
			&& offering.TargetLevel == targetLevel);
		return listing.Pricing.FixedCost;
	}

	private static int Credits(ResourceBundle bundle)
	{
		Assert.True(bundle.TryGet(ResourceId.Credits, out var amount));
		return amount;
	}
}
