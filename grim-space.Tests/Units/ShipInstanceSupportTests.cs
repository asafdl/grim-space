using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Specs;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Units;

public sealed class ShipInstanceSupportTests
{
	private static ShipInstance FullCatalogFighter(string id) =>
		ShipInstance.FromSpec(id, FighterSpec.Instance, ShipCatalog.FullFighterLoadout());

	[Fact]
	public void TryWithUpgradedMaxShields_IncreasesMaxAndCurrentShields()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var previousMax = ship.Loadout.MaxShieldPoints[ESpatialOrientation.Forward];

		Assert.True(ship.TryWithUpgradedMaxShields(ESpatialOrientation.Forward, out var after));

		Assert.Equal(1, after.Loadout.ShieldUpgradeTiers[ESpatialOrientation.Forward]);
		Assert.Equal(previousMax + 1, after.Loadout.MaxShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(previousMax + 1, after.ShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(ship.Loadout.MaxShieldPoints[ESpatialOrientation.Port], after.Loadout.MaxShieldPoints[ESpatialOrientation.Port]);
		Assert.Equal(ship.ShieldPoints[ESpatialOrientation.Port], after.ShieldPoints[ESpatialOrientation.Port]);
	}

	[Fact]
	public void TryWithUpgradedMaxShields_WhenCurrentBelowMax_RaisesCurrentWithoutExceedingNewCap()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var previousMax = ship.Loadout.MaxShieldPoints[ESpatialOrientation.Forward];
		ship.ShieldPoints[ESpatialOrientation.Forward] = 0;
		var previousPort = ship.ShieldPoints[ESpatialOrientation.Port];

		Assert.True(ship.TryWithUpgradedMaxShields(ESpatialOrientation.Forward, out var after));

		Assert.Equal(previousMax + 1, after.Loadout.MaxShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(1, after.ShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(previousPort, after.ShieldPoints[ESpatialOrientation.Port]);
	}

	[Fact]
	public void TryWithUpgradedMaxShields_ZeroMaxFaceBecomesShieldedAndStopsAtPerFaceCap()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var face = ESpatialOrientation.Dorsal;
		ship.Loadout.MaxShieldPoints[face] = 0;
		ship.ShieldPoints[face] = 0;

		for (var tier = 1; tier <= ShipLoadout.MaxShieldUpgradeTier; tier++)
		{
			Assert.True(ship.TryWithUpgradedMaxShields(face, out var after));
			Assert.Equal(tier, after.Loadout.ShieldUpgradeTiers[face]);
			Assert.Equal(tier, after.Loadout.MaxShieldPoints[face]);
			Assert.Equal(tier, after.ShieldPoints[face]);
			ship = after;
		}

		Assert.False(ship.TryWithUpgradedMaxShields(face, out _));
		Assert.True(ship.TryWithUpgradedMaxShields(ESpatialOrientation.Forward, out _));
	}

	[Fact]
	public void ShieldUpgradeTiers_AreIndependentAcrossCopiesAndValidateBounds()
	{
		var loadout = ShipCatalog.NewRunLoadoutFor(EType.Fighter);
		var tiers = new GrimSpace.Units.Loadouts.Defenses.FaceShieldPoints();
		tiers[ESpatialOrientation.Port] = 2;
		var upgraded = ShipLoadout.Create(
			FighterSpec.Instance,
			loadout.MaxHullPoints,
			loadout.MaxShieldPoints,
			loadout.InstalledAbilities,
			tiers);
		tiers[ESpatialOrientation.Port] = 0;
		var copy = upgraded.DeepCopy();
		copy.ShieldUpgradeTiers[ESpatialOrientation.Port] = 1;

		Assert.Equal(2, upgraded.ShieldUpgradeTiers[ESpatialOrientation.Port]);
		Assert.Equal(1, copy.ShieldUpgradeTiers[ESpatialOrientation.Port]);
		tiers[ESpatialOrientation.Port] = -1;
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			ShipLoadout.Create(FighterSpec.Instance, loadout.MaxHullPoints, loadout.MaxShieldPoints, loadout.InstalledAbilities, tiers));
		tiers[ESpatialOrientation.Port] = ShipLoadout.MaxShieldUpgradeTier + 1;
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			ShipLoadout.Create(FighterSpec.Instance, loadout.MaxHullPoints, loadout.MaxShieldPoints, loadout.InstalledAbilities, tiers));
	}

	[Fact]
	public void TryWithDamageUpgraded_ReplacesOnlyTargetMountAndBumpsDamageTier()
	{
		var ship = FullCatalogFighter("fighter-1");
		var mount = new AbilityMount(EAbilityKind.Flak, ESpatialOrientation.Port);

		Assert.True(ship.TryWithDamageUpgraded(mount, out var after));

		var port = after.Loadout.InstalledAbilities.First(a => a.MountedOn == ESpatialOrientation.Port);
		Assert.IsType<FlakSpec>(port.Spec);
		Assert.Equal(2, ((FlakSpec)port.Spec).Damage);
		Assert.Equal(1, ((FlakSpec)port.Spec).DamageUpgradeTier);

		var starboard = after.Loadout.InstalledAbilities.First(a => a.MountedOn == ESpatialOrientation.Starboard);
		Assert.Equal(0, ((FlakSpec)starboard.Spec).DamageUpgradeTier);
	}

	[Fact]
	public void TryWithRangeUpgraded_IncreasesFlakBurstRange()
	{
		var ship = FullCatalogFighter("fighter-1");
		var mount = new AbilityMount(EAbilityKind.Flak, ESpatialOrientation.Port);
		var beforeRange = ((FlakSpec)ship.Loadout.InstalledAbilities.First(a => a.Mount == mount).Spec).BurstRange;

		Assert.True(ship.TryWithRangeUpgraded(mount, out var after));

		var flak = (FlakSpec)after.Loadout.InstalledAbilities.First(a => a.Mount == mount).Spec;
		Assert.Equal(beforeRange + 1, flak.BurstRange);
		Assert.Equal(1, flak.RangeUpgradeTier);
	}

	[Fact]
	public void TryWithDamageUpgraded_StopsAtMaxTier()
	{
		var ship = FullCatalogFighter("fighter-1");
		var mount = new AbilityMount(EAbilityKind.Flak, ESpatialOrientation.Port);

		for (var tier = 0; tier < FlakSpec.MaxDamageUpgradeTier; tier++)
			Assert.True(ship.TryWithDamageUpgraded(mount, out ship));

		Assert.False(ship.TryWithDamageUpgraded(mount, out _));
	}

	[Fact]
	public void TryWithInstalledAbility_AddsMountWhenFacetIsOpen()
	{
		var mount = new AbilityMount(EAbilityKind.Flak, ESpatialOrientation.Port);
		var baseline = FighterSpec.Instance.BaselineFor(mount);
		var loadout = ShipLoadout.Create(
			FighterSpec.Instance,
			FighterSpec.Instance.DefaultMaxHullPoints,
			FighterSpec.Instance.DefaultMaxShieldPoints,
			[
				new InstalledAbility(
					FighterSpec.Instance.BaselineFor(new AbilityMount(EAbilityKind.Railgun, ESpatialOrientation.Forward)),
					ESpatialOrientation.Forward),
			]);
		var ship = ShipInstance.FromSpec("fighter-partial", FighterSpec.Instance, loadout);
		var installed = new InstalledAbility(baseline, ESpatialOrientation.Port);

		Assert.True(ship.TryWithInstalledAbility(installed, out var after));
		Assert.Contains(after.Loadout.InstalledAbilities, a => a.Mount == installed.Mount);
	}

	[Fact]
	public void TryWithInstalledAbility_RejectsOccupiedFacet()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var mount = new AbilityMount(EAbilityKind.TorpedoLauncher, ESpatialOrientation.Ventral);
		var installed = new InstalledAbility(FighterSpec.Instance.BaselineFor(mount), ESpatialOrientation.Ventral);

		Assert.False(ship.TryWithInstalledAbility(installed, out _));
	}

	[Fact]
	public void TryWithUpgradedMaxHull_IncreasesCapacityWithoutChangingCurrentHull()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		ship.HullPoints = 1;
		var previousMax = ship.Loadout.MaxHullPoints;

		Assert.True(ship.TryWithUpgradedMaxHull(out var after));

		Assert.Equal(1, after.Loadout.HullUpgradeTier);
		Assert.Equal(previousMax + 1, after.Loadout.MaxHullPoints);
		Assert.Equal(1, after.HullPoints);
	}

	[Fact]
	public void TryWithUpgradedMaxHull_StopsAtMaxTier()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);

		for (var tier = 0; tier < ShipLoadout.MaxHullUpgradeTier; tier++)
			Assert.True(ship.TryWithUpgradedMaxHull(out ship));

		Assert.False(ship.TryWithUpgradedMaxHull(out _));
	}
}
