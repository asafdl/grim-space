using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Specs;
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
	public void ListFor_PartialFighterLoadout_OffersScrapDroneSwarmInstallOnOpenFacets()
	{
		var ship = FighterWithLightningCannonOnly("fighter-partial");
		var offers = WeaponsCatalog.ListFor(ship);

		Assert.Contains(
			offers,
			offer => offer.Offering == new MerchantCatalog.Offering(
				MerchantCatalog.Kind.InstallWeapon,
				new AbilityMount(EAbilityKind.ScrapDroneSwarm, ESpatialOrientation.Port)));
		Assert.Contains(
			offers,
			offer => offer.Offering == new MerchantCatalog.Offering(
				MerchantCatalog.Kind.InstallWeapon,
				new AbilityMount(EAbilityKind.ScrapDroneSwarm, ESpatialOrientation.Starboard)));
		Assert.DoesNotContain(
			offers,
			offer => offer.Offering.Kind == MerchantCatalog.Kind.InstallWeapon
				&& offer.Offering.Mount?.Kind == EAbilityKind.LightningCannon);
	}

	[Fact]
	public void ListFor_Patrol_DoesNotOfferLightningCannonWithoutChassisSlot()
	{
		var ship = ShipInstance.FromCatalog("patrol-1", EType.Patrol);
		var offers = WeaponsCatalog.ListFor(ship);

		Assert.DoesNotContain(
			offers,
			offer => offer.Offering.Kind == MerchantCatalog.Kind.InstallWeapon
				&& offer.Offering.Mount?.Kind == EAbilityKind.LightningCannon);
	}

	[Fact]
	public void ListFor_DamageOfferPriceIncreasesAfterUpgrade()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var offering = MerchantPurchaseTestHarness.ScrapDroneSwarmPortDamageUpgrade;
		var firstOffer = WeaponsCatalog.ListFor(ship).Single(o => o.Offering == offering);
		Assert.True(ship.TryWithDamageUpgraded(
			new AbilityMount(EAbilityKind.ScrapDroneSwarm, ESpatialOrientation.Port),
			out ship));
		var secondOffer = WeaponsCatalog.ListFor(ship).Single(o => o.Offering == offering);
		Assert.True(firstOffer.Cost.TryGet(ResourceId.ScrapAlloy, out var firstScrap));
		Assert.True(secondOffer.Cost.TryGet(ResourceId.ScrapAlloy, out var secondScrap));
		Assert.True(secondScrap > firstScrap);
	}

	[Fact]
	public void ListFor_DamageTierZero_MatchesEconomyTable()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var offering = MerchantPurchaseTestHarness.ScrapDroneSwarmPortDamageUpgrade;
		var offer = WeaponsCatalog.ListFor(ship).Single(o => o.Offering == offering);
		Assert.Equal(MerchantUpgradePricing.WeaponDamageUpgrade(0), offer.Cost);
	}

	[Fact]
	public void ListFor_RangeTierOne_MatchesEconomyTable()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		Assert.True(ship.TryWithRangeUpgraded(
			new AbilityMount(EAbilityKind.ScrapDroneSwarm, ESpatialOrientation.Port),
			out ship));
		var offering = MerchantPurchaseTestHarness.ScrapDroneSwarmPortRangeUpgrade;
		var offer = WeaponsCatalog.ListFor(ship).Single(o => o.Offering == offering);
		Assert.Equal(MerchantUpgradePricing.WeaponRangeUpgrade(1), offer.Cost);
	}

	[Fact]
	public void ListFor_AfterInstallCommit_RemovesInstallOfferAndListsUpgrades()
	{
		var ship = FighterWithLightningCannonOnly("fighter-partial");
		var install = new MerchantCatalog.Offering(
			MerchantCatalog.Kind.InstallWeapon,
			new AbilityMount(EAbilityKind.ScrapDroneSwarm, ESpatialOrientation.Port));
		Assert.Contains(WeaponsCatalog.ListFor(ship), offer => offer.Offering == install);

		var baseline = FighterSpec.Instance.BaselineFor(install.Mount!.Value);
		Assert.True(ship.TryWithInstalledAbility(
			new InstalledAbility(baseline, ESpatialOrientation.Port),
			out ship));

		var offers = WeaponsCatalog.ListFor(ship);
		Assert.DoesNotContain(offers, offer => offer.Offering == install);
		Assert.Contains(
			offers,
			offer => offer.Offering == new MerchantCatalog.Offering(
				MerchantCatalog.Kind.UpgradeDamage,
				new AbilityMount(EAbilityKind.ScrapDroneSwarm, ESpatialOrientation.Port)));
	}

	private static ShipInstance FighterWithLightningCannonOnly(string id)
	{
		var mount = new AbilityMount(EAbilityKind.LightningCannon, ESpatialOrientation.Forward);
		var baseline = FighterSpec.Instance.BaselineFor(mount);
		var loadout = ShipLoadout.Create(
			FighterSpec.Instance,
			FighterSpec.Instance.DefaultMaxHullPoints,
			FighterSpec.Instance.DefaultMaxShieldPoints,
			[new InstalledAbility(baseline, ESpatialOrientation.Forward)]);
		return ShipInstance.FromSpec(id, FighterSpec.Instance, loadout);
	}
}
