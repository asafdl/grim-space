using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

[StarSystemTestSuite]
public sealed class WeaponsCatalogTests
{
	[Fact]
	public void ListFor_FighterIncludesShieldAndAbilityOffers()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var offers = WeaponsCatalog.ListFor(ship);

		Assert.Contains(offers, offer => offer.Category == EWeaponsOfferCategory.MaxShields);
		Assert.Equal(3, offers.Count(offer => offer.Category == EWeaponsOfferCategory.Ability));
	}

	[Fact]
	public void ListFor_AbilityOfferPriceIncreasesAfterUpgrade()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var offerId = WeaponsCatalog.AbilityOfferId(
			new AbilityMount(EAbilityKind.Flak, ESpatialOrientation.Port));
		Assert.True(WeaponsCatalog.TryQuote(offerId, ship, out var firstCost));
		Assert.True(ship.TryWithUpgradedAbility(
			new AbilityMount(EAbilityKind.Flak, ESpatialOrientation.Port),
			out ship));
		Assert.True(WeaponsCatalog.TryQuote(offerId, ship, out var secondCost));
		Assert.True(firstCost.TryGet(ResourceId.ScrapAlloy, out var firstScrap));
		Assert.True(secondCost.TryGet(ResourceId.ScrapAlloy, out var secondScrap));
		Assert.True(secondScrap > firstScrap);
	}

	[Fact]
	public void TryQuote_UsesTierBasedAbilityPricing()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var offerId = WeaponsCatalog.AbilityOfferId(
			new AbilityMount(EAbilityKind.Flak, ESpatialOrientation.Port));
		Assert.True(WeaponsCatalog.TryQuote(offerId, ship, out var cost));
		Assert.True(cost.TryGet(ResourceId.ScrapAlloy, out var scrap));
		Assert.Equal(MerchantPricingRules.ScrapForAbilityUpgrade(0), scrap);
	}

	[Fact]
	public void ListFor_AtMaxShieldTier_ExcludesMaxShieldOffer()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		for (var tier = 0; tier < MerchantPricingRules.MaxShieldUpgradeTier; tier++)
		{
			var offer = WeaponsCatalog.ListFor(ship).Single(o => o.Category == EWeaponsOfferCategory.MaxShields);
			Assert.True(ship.TryWithUpgradedMaxShields(out ship));
		}

		Assert.Equal(MerchantPricingRules.MaxShieldUpgradeTier, ship.Spec.ShieldUpgradeTier);
		Assert.DoesNotContain(
			WeaponsCatalog.ListFor(ship),
			offer => offer.Category == EWeaponsOfferCategory.MaxShields);
	}
}
