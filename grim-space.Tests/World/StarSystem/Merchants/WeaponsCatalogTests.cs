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
	public void ListFor_Fighter_ExcludesShields_IncludesDamageAndRangeUpgrades()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var offers = WeaponsCatalog.ListFor(ship);

		Assert.DoesNotContain(
			offers,
			offer => offer.Offering.Kind == MerchantCatalog.Kind.UpgradeMaxShields);
		Assert.Equal(3, offers.Count(offer => offer.Offering.Kind == MerchantCatalog.Kind.UpgradeDamage));
		Assert.Equal(3, offers.Count(offer => offer.Offering.Kind == MerchantCatalog.Kind.UpgradeRange));
	}

	[Fact]
	public void ListFor_Patrol_IncludesRailgunInstallOnForward()
	{
		var ship = ShipInstance.FromCatalog("patrol-1", EType.Patrol);
		var offers = WeaponsCatalog.ListFor(ship);

		var install = MerchantPurchaseTestHarness.RailgunForwardInstall;
		var installOffer = offers.Single(o => o.Offering == install);
		Assert.True(installOffer.Cost.TryGet(ResourceId.ScrapAlloy, out var scrap));
		Assert.Equal(80, scrap);
	}

	[Fact]
	public void ListFor_DamageOfferPriceIncreasesAfterUpgrade()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var offering = MerchantPurchaseTestHarness.FlakPortDamageUpgrade;
		var firstOffer = WeaponsCatalog.ListFor(ship).Single(o => o.Offering == offering);
		Assert.True(ship.TryWithDamageUpgraded(
			new AbilityMount(EAbilityKind.Flak, ESpatialOrientation.Port),
			out ship));
		var secondOffer = WeaponsCatalog.ListFor(ship).Single(o => o.Offering == offering);
		Assert.True(firstOffer.Cost.TryGet(ResourceId.ScrapAlloy, out var firstScrap));
		Assert.True(secondOffer.Cost.TryGet(ResourceId.ScrapAlloy, out var secondScrap));
		Assert.True(secondScrap > firstScrap);
	}

	[Fact]
	public void ListFor_DamageTierZeroCostsFortyScrap()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var offering = MerchantPurchaseTestHarness.FlakPortDamageUpgrade;
		var offer = WeaponsCatalog.ListFor(ship).Single(o => o.Offering == offering);
		Assert.True(offer.Cost.TryGet(ResourceId.ScrapAlloy, out var scrap));
		Assert.Equal(40, scrap);
	}

	[Fact]
	public void ListFor_RangeTierOneCostsSixtyScrap()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		Assert.True(ship.TryWithRangeUpgraded(
			new AbilityMount(EAbilityKind.Flak, ESpatialOrientation.Port),
			out ship));
		var offering = MerchantPurchaseTestHarness.FlakPortRangeUpgrade;
		var offer = WeaponsCatalog.ListFor(ship).Single(o => o.Offering == offering);
		Assert.True(offer.Cost.TryGet(ResourceId.ScrapAlloy, out var scrap));
		Assert.Equal(60, scrap);
	}

	[Fact]
	public void ListFor_AfterInstallCommit_RemovesInstallOfferAndListsUpgrades()
	{
		var ship = ShipInstance.FromCatalog("patrol-1", EType.Patrol);
		var install = MerchantPurchaseTestHarness.RailgunForwardInstall;
		Assert.Contains(WeaponsCatalog.ListFor(ship), offer => offer.Offering == install);

		var spec = ShipCatalog.DefaultAbilitySpec(EType.Fighter, EAbilityKind.Railgun)!;
		Assert.True(ship.TryWithInstalledAbility(
			new InstalledAbility(spec, ESpatialOrientation.Forward),
			out ship));

		var offers = WeaponsCatalog.ListFor(ship);
		Assert.DoesNotContain(offers, offer => offer.Offering == install);
		Assert.Contains(
			offers,
			offer => offer.Offering == new MerchantCatalog.Offering(
				MerchantCatalog.Kind.UpgradeDamage,
				new AbilityMount(EAbilityKind.Railgun, ESpatialOrientation.Forward)));
	}
}
