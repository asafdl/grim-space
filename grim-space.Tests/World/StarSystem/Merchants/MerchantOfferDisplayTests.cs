using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Presentation.Facilities;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

[StarSystemTestSuite]
public sealed class MerchantOfferDisplayTests
{
	[Theory]
	[InlineData(MerchantCatalog.Kind.UpgradeDamage, 1, "Damage upgrade pack · Level 1")]
	[InlineData(MerchantCatalog.Kind.UpgradeRange, 2, "Range upgrade pack · Level 2")]
	[InlineData(MerchantCatalog.Kind.UpgradeMaxShields, 3, "Shield upgrade pack · Level 3")]
	public void PackTitle_SeparatesTargetLevelFromShopTier(
		MerchantCatalog.Kind kind,
		int targetTier,
		string expected)
	{
		Assert.Equal(
			expected,
			MerchantOfferDisplay.PackTitle(new MerchantCatalog.Offering(kind, targetTier)));
	}

	[Fact]
	public void AbilityUpgradeBody_ShowsCurrentAndTargetLevel()
	{
		var installed = new InstalledAbility(
			EAbilityKind.LightningCannon,
			ESpatialOrientation.Forward);

		Assert.Equal(
			"Upgrade level 0 \u2192 2",
			MerchantOfferDisplay.AbilityUpgradeBody(
				new MerchantCatalog.Offering(MerchantCatalog.Kind.UpgradeDamage, 2),
				installed));
	}

	[Fact]
	public void ShieldUpgradeBody_ShowsResultingCapacity()
	{
		var ship = ShipInstance.FromCatalog("fighter", EType.Fighter);
		var face = ESpatialOrientation.Retro;
		var current = ship.Loadout.MaxShieldPoints[face];

		Assert.Equal(
			$"Max shields {current} \u2192 2",
			MerchantOfferDisplay.ShieldUpgradeBody(ship, face, 2));
	}

	[Fact]
	public void ServiceBodies_ShowResultingState()
	{
		Assert.Equal("Hull integrity 4 \u2192 8", MerchantOfferDisplay.HullRepairBody(4, 8));
		Assert.Equal("Shield charge 1 \u2192 3", MerchantOfferDisplay.ShieldRechargeBody(1, 3));
		Assert.Equal("Total shields 10 \u2192 18", MerchantOfferDisplay.RechargeAllBody(10, 18));
	}

	[Theory]
	[InlineData(EMerchantAvailabilityBand.T0, "Shop tier T0")]
	[InlineData(EMerchantAvailabilityBand.T0_5, "Shop tier T0.5")]
	[InlineData(EMerchantAvailabilityBand.T1, "Shop tier T1")]
	public void BandLabel_UsesShopTierTerminology(
		EMerchantAvailabilityBand band,
		string expected)
	{
		Assert.Equal(expected, MerchantOfferDisplay.BandLabel(band));
	}
}
