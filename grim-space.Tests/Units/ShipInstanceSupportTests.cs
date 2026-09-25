using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Units;

public sealed class ShipInstanceSupportTests
{
	[Fact]
	public void TryWithUpgradedMaxShields_IncreasesMaxAndCurrentShields()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var previousMax = ship.Spec.MaxShieldPoints[ESpatialOrientation.Forward];

		Assert.True(ship.TryWithUpgradedMaxShields(ESpatialOrientation.Forward, out var after));

		Assert.Equal(1, after.Spec.ShieldUpgradeTiers[ESpatialOrientation.Forward]);
		Assert.Equal(previousMax + 1, after.Spec.MaxShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(previousMax + 1, after.ShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(ship.Spec.MaxShieldPoints[ESpatialOrientation.Port], after.Spec.MaxShieldPoints[ESpatialOrientation.Port]);
		Assert.Equal(ship.ShieldPoints[ESpatialOrientation.Port], after.ShieldPoints[ESpatialOrientation.Port]);
	}

	[Fact]
	public void TryWithUpgradedMaxShields_WhenCurrentBelowMax_RaisesCurrentWithoutExceedingNewCap()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var previousMax = ship.Spec.MaxShieldPoints[ESpatialOrientation.Forward];
		ship.ShieldPoints[ESpatialOrientation.Forward] = 0;
		var previousPort = ship.ShieldPoints[ESpatialOrientation.Port];

		Assert.True(ship.TryWithUpgradedMaxShields(ESpatialOrientation.Forward, out var after));

		Assert.Equal(previousMax + 1, after.Spec.MaxShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(1, after.ShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(previousPort, after.ShieldPoints[ESpatialOrientation.Port]);
	}

	[Fact]
	public void TryWithUpgradedMaxShields_ZeroMaxFaceBecomesShieldedAndStopsAtPerFaceCap()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var face = ESpatialOrientation.Dorsal;
		ship.Spec.MaxShieldPoints[face] = 0;
		ship.ShieldPoints[face] = 0;

		for (var tier = 1; tier <= ShipSpec.MaxShieldUpgradeTier; tier++)
		{
			Assert.True(ship.TryWithUpgradedMaxShields(face, out var after));
			Assert.Equal(tier, after.Spec.ShieldUpgradeTiers[face]);
			Assert.Equal(tier, after.Spec.MaxShieldPoints[face]);
			Assert.Equal(tier, after.ShieldPoints[face]);
			ship = after;
		}

		Assert.False(ship.TryWithUpgradedMaxShields(face, out _));
		Assert.True(ship.TryWithUpgradedMaxShields(ESpatialOrientation.Forward, out _));
	}

	[Fact]
	public void ShieldUpgradeTiers_AreIndependentAcrossCopiesAndValidateBounds()
	{
		var spec = ShipCatalog.DefaultFor(EType.Fighter);
		var tiers = new GrimSpace.Units.Loadouts.Defenses.FaceShieldPoints();
		tiers[ESpatialOrientation.Port] = 2;
		var upgraded = ShipSpec.Create(spec.Chassis, spec.MaxHullPoints, spec.MaxShieldPoints, spec.InstalledAbilities, tiers);
		tiers[ESpatialOrientation.Port] = 0;
		var copy = upgraded.DeepCopy();
		copy.ShieldUpgradeTiers[ESpatialOrientation.Port] = 1;

		Assert.Equal(2, upgraded.ShieldUpgradeTiers[ESpatialOrientation.Port]);
		Assert.Equal(1, copy.ShieldUpgradeTiers[ESpatialOrientation.Port]);
		tiers[ESpatialOrientation.Port] = -1;
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			ShipSpec.Create(spec.Chassis, spec.MaxHullPoints, spec.MaxShieldPoints, spec.InstalledAbilities, tiers));
		tiers[ESpatialOrientation.Port] = ShipSpec.MaxShieldUpgradeTier + 1;
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			ShipSpec.Create(spec.Chassis, spec.MaxHullPoints, spec.MaxShieldPoints, spec.InstalledAbilities, tiers));
	}

	[Fact]
	public void TryWithDamageUpgraded_ReplacesOnlyTargetMountAndBumpsDamageTier()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var mount = new AbilityMount(EAbilityKind.Flak, ESpatialOrientation.Port);

		Assert.True(ship.TryWithDamageUpgraded(mount, out var after));

		var port = after.Spec.InstalledAbilities.First(a => a.MountedOn == ESpatialOrientation.Port);
		Assert.IsType<FlakSpec>(port.Spec);
		Assert.Equal(2, ((FlakSpec)port.Spec).Damage);
		Assert.Equal(1, ((FlakSpec)port.Spec).DamageUpgradeTier);

		var starboard = after.Spec.InstalledAbilities.First(a => a.MountedOn == ESpatialOrientation.Starboard);
		Assert.Equal(0, ((FlakSpec)starboard.Spec).DamageUpgradeTier);
	}

	[Fact]
	public void TryWithRangeUpgraded_IncreasesFlakBurstRange()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var mount = new AbilityMount(EAbilityKind.Flak, ESpatialOrientation.Port);
		var beforeRange = ((FlakSpec)ship.Spec.InstalledAbilities.First(a => a.Mount == mount).Spec).BurstRange;

		Assert.True(ship.TryWithRangeUpgraded(mount, out var after));

		var flak = (FlakSpec)after.Spec.InstalledAbilities.First(a => a.Mount == mount).Spec;
		Assert.Equal(beforeRange + 1, flak.BurstRange);
		Assert.Equal(1, flak.RangeUpgradeTier);
	}

	[Fact]
	public void TryWithDamageUpgraded_StopsAtMaxTier()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var mount = new AbilityMount(EAbilityKind.Flak, ESpatialOrientation.Port);

		for (var tier = 0; tier < FlakSpec.MaxDamageUpgradeTier; tier++)
			Assert.True(ship.TryWithDamageUpgraded(mount, out ship));

		Assert.False(ship.TryWithDamageUpgraded(mount, out _));
	}

	[Fact]
	public void TryWithInstalledAbility_AddsMountWhenFacetIsOpen()
	{
		var ship = ShipInstance.FromCatalog("patrol-1", EType.Patrol);
		var spec = ShipCatalog.DefaultAbilitySpec(EType.Fighter, EAbilityKind.Railgun)!;
		var installed = new InstalledAbility(spec, ESpatialOrientation.Forward);

		Assert.True(ship.TryWithInstalledAbility(installed, out var after));
		Assert.Contains(after.Spec.InstalledAbilities, a => a.Mount == installed.Mount);
	}

	[Fact]
	public void TryWithInstalledAbility_RejectsOccupiedFacet()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var spec = ShipCatalog.DefaultAbilitySpec(EType.Fighter, EAbilityKind.Flak)!;
		var installed = new InstalledAbility(spec, ESpatialOrientation.Port);

		Assert.False(ship.TryWithInstalledAbility(installed, out _));
	}

	[Fact]
	public void TryWithUpgradedMaxHull_IncreasesCapacityWithoutChangingCurrentHull()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		ship.HullPoints = 1;
		var previousMax = ship.Spec.MaxHullPoints;

		Assert.True(ship.TryWithUpgradedMaxHull(out var after));

		Assert.Equal(1, after.Spec.HullUpgradeTier);
		Assert.Equal(previousMax + 1, after.Spec.MaxHullPoints);
		Assert.Equal(1, after.HullPoints);
	}

	[Fact]
	public void TryWithUpgradedMaxHull_StopsAtMaxTier()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);

		for (var tier = 0; tier < ShipSpec.MaxHullUpgradeTier; tier++)
			Assert.True(ship.TryWithUpgradedMaxHull(out ship));

		Assert.False(ship.TryWithUpgradedMaxHull(out _));
	}
}
