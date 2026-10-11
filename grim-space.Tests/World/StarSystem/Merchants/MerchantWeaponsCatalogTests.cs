using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Specs;
using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

[StarSystemTestSuite]
public sealed class MerchantWeaponsCatalogTests
{
	[Fact]
	public void T0_HasNoWeaponSheet()
	{
		var ship = FullFighter();

		Assert.Empty(MerchantCatalog.ListFor(
			EMerchantCatalog.Weapons,
			ship,
			EMerchantAvailabilityBand.T0));
	}

	[Fact]
	public void T1_ListsOneGenericDamagePackWithEligibleWeaponTargets()
	{
		var ship = FullFighter();
		var offer = Assert.Single(MerchantCatalog.ListFor(
			EMerchantCatalog.Weapons,
			ship,
			EMerchantAvailabilityBand.T1));

		Assert.Equal(
			new MerchantCatalog.Offering(MerchantCatalog.Kind.UpgradeDamage, 1),
			offer.Offering);
		Assert.Equal(3, offer.Targets.Count);
		Assert.Contains(MerchantPurchaseTestHarness.ScrapDroneSwarmPortTarget, offer.Targets);
	}

	[Fact]
	public void T1_5_CombinesT1AndT1_5Sheets()
	{
		var ship = FullFighter();

		var offer = Assert.Single(MerchantCatalog.ListFor(
			EMerchantCatalog.Weapons,
			ship,
			EMerchantAvailabilityBand.T1_5));

		Assert.Equal(MerchantCatalog.Kind.UpgradeDamage, offer.Offering.Kind);
	}

	[Fact]
	public void GenericPack_ExcludesWeaponsAlreadyAtTargetTier()
	{
		var ship = FullFighter();
		var target = MerchantPurchaseTestHarness.ScrapDroneSwarmPortTarget;
		Assert.True(ship.TryWithDamageUpgradeTier(target.Mount!.Value, 1, out ship));

		var offer = Assert.Single(MerchantCatalog.ListFor(
			EMerchantCatalog.Weapons,
			ship,
			EMerchantAvailabilityBand.T1));

		Assert.DoesNotContain(target, offer.Targets);
		Assert.NotEmpty(offer.Targets);
	}

	private static ShipInstance FullFighter() =>
		ShipInstance.FromSpec(
			"fighter-1",
			FighterSpec.Instance,
			ShipCatalog.FullFighterLoadout());
}
