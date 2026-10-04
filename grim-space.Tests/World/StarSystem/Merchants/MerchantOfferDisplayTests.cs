using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Specs;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Presentation.Facilities;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

[StarSystemTestSuite]
public sealed class MerchantOfferDisplayTests
{
	[Fact]
	public void AbilitiesFor_GroupsOffersByExactMount()
	{
		var ship = ShipInstance.FromCatalog("fighter", EType.Fighter);
		var port = new AbilityMount(EAbilityKind.ScrapDroneSwarm, ESpatialOrientation.Port);
		var lightning = new AbilityMount(EAbilityKind.LightningCannon, ESpatialOrientation.Port);
		var offers = WeaponsCatalog.ListFor(ship).Append(new MerchantCatalog.Offer(
			new MerchantCatalog.Offering(MerchantCatalog.Kind.InstallWeapon, lightning),
			MerchantUpgradePricing.LightningCannonInstall())).ToArray();

		var abilities = MerchantOfferDisplay.AbilitiesFor(ship, offers);

		Assert.Equal(abilities.Length, abilities.Select(ability => ability.Mount).Distinct().Count());
		var portDrones = Assert.Single(abilities, ability => ability.Mount == port);
		Assert.True(portDrones.IsInstalled);
		Assert.Equal(2, portDrones.Offers.Count);
		Assert.All(portDrones.Offers, offer => Assert.Equal(port, offer.Offering.Mount));
		var portLightning = Assert.Single(abilities, ability => ability.Mount == lightning);
		Assert.False(portLightning.IsInstalled);
		Assert.Equal(lightning, Assert.Single(portLightning.Offers).Offering.Mount);
	}

	[Fact]
	public void AbilitiesFor_KeepsInstalledAbilitiesWithoutOffers()
	{
		var ship = ShipInstance.FromCatalog("carrier", EType.Carrier);
		var abilities = MerchantOfferDisplay.AbilitiesFor(ship, WeaponsCatalog.ListFor(ship));

		var miner = Assert.Single(abilities, ability => ability.Mount.Kind == EAbilityKind.MinerBay);
		Assert.True(miner.IsInstalled);
		Assert.Empty(miner.Offers);
	}

	[Fact]
	public void AbilitiesFor_InstallRefreshKeepsMountAndReplacesOffers()
	{
		var spec = FighterSpec.Instance;
		var loadout = ShipLoadout.Create(spec, spec.DefaultMaxHullPoints, spec.DefaultMaxShieldPoints, []);
		var ship = ShipInstance.FromSpec("fighter", spec, loadout);
		var mount = new AbilityMount(EAbilityKind.LightningCannon, ESpatialOrientation.Forward);
		var before = MerchantOfferDisplay.AbilitiesFor(ship, WeaponsCatalog.ListFor(ship));
		var install = Assert.Single(before, ability => ability.Mount == mount);
		Assert.False(install.IsInstalled);
		Assert.Equal(MerchantCatalog.Kind.InstallWeapon, Assert.Single(install.Offers).Offering.Kind);

		Assert.True(ship.TryWithInstalledAbility(new InstalledAbility(spec.BaselineFor(mount), mount.Facet), out ship));
		var after = MerchantOfferDisplay.AbilitiesFor(ship, WeaponsCatalog.ListFor(ship));
		var installed = Assert.Single(after, ability => ability.Mount == mount);
		Assert.True(installed.IsInstalled);
		Assert.Equal(2, installed.Offers.Count);
		Assert.DoesNotContain(installed.Offers, offer => offer.Offering.Kind == MerchantCatalog.Kind.InstallWeapon);
	}

	[Fact]
	public void AbilitiesFor_EmptyCatalogAndLoadout_HasNoChoices()
	{
		var spec = CarrierSpec.Instance;
		var loadout = ShipLoadout.Create(spec, spec.DefaultMaxHullPoints, spec.DefaultMaxShieldPoints, []);
		var ship = ShipInstance.FromSpec("carrier", spec, loadout);

		Assert.Empty(MerchantOfferDisplay.AbilitiesFor(ship, []));
	}

	[Theory]
	[InlineData(EAbilityKind.ScrapDroneSwarm)]
	[InlineData(EAbilityKind.LightningCannon)]
	[InlineData(EAbilityKind.MinerBay)]
	[InlineData(EAbilityKind.VoidBombLauncher)]
	public void AbilityMetadata_CoversEachSupportedKind(EAbilityKind kind)
	{
		Assert.NotEqual(kind.ToString(), MerchantOfferDisplay.KindLabel(kind));
		Assert.StartsWith("res://assets/ui/abilities/", MerchantOfferDisplay.AbilityIconPath(kind));
	}

	[Theory]
	[InlineData(0, 0, "Damage \u00b7 Mk 1", "Range \u00b7 Mk 1")]
	[InlineData(1, 2, "Damage \u00b7 Mk 2", "Range \u00b7 Mk 3")]
	public void UpgradeTitles_ContainOnlyStatAndNextTier(
		int damageTier, int rangeTier, string damageTitle, string rangeTitle)
	{
		var spec = new LightningCannonSpec(1, 2, 5, 0)
		{
			DamageUpgradeTier = damageTier,
			RangeUpgradeTier = rangeTier,
		};

		Assert.Equal(damageTitle, MerchantOfferDisplay.DamageUpgradeTitle(spec));
		Assert.Equal(rangeTitle, MerchantOfferDisplay.RangeUpgradeTitle(spec));
	}

	[Theory]
	[InlineData(2, 5, "Shot damage 2 \u2192 3", "Line length 5 \u2192 6 cells")]
	[InlineData(4, 7, "Shot damage 4 \u2192 5", "Line length 7 \u2192 8 cells")]
	public void LightningUpgradeBody_ShowsCurrentAndUpgradedStats(
		int damage, int range, string damageBody, string rangeBody)
	{
		var spec = new LightningCannonSpec(1, damage, range, 0);

		Assert.Equal(damageBody, MerchantOfferDisplay.DamageUpgradeBody(spec));
		Assert.Equal(rangeBody, MerchantOfferDisplay.RangeUpgradeBody(spec));
	}

	[Theory]
	[InlineData(1, 2, "Burst damage 1 \u2192 2", "Burst range 2 \u2192 3 cells")]
	[InlineData(3, 4, "Burst damage 3 \u2192 4", "Burst range 4 \u2192 5 cells")]
	public void DroneUpgradeBody_ShowsCurrentAndUpgradedStats(
		int damage, int range, string damageBody, string rangeBody)
	{
		var spec = new ScrapDroneSwarmSpec(1, damage, range);

		Assert.Equal(damageBody, MerchantOfferDisplay.DamageUpgradeBody(spec));
		Assert.Equal(rangeBody, MerchantOfferDisplay.RangeUpgradeBody(spec));
	}
}
