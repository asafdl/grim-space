using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Specs;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Units;

public sealed class ShipInstanceSupportTests
{
	private static ShipInstance FullCatalogFighter(string id) =>
		ShipInstance.FromCatalog(id, EType.RepurposedMiner);

	private static ShipInstance LightningCannonFighter(
		string id,
		int damageUpgradeTier = 0,
		int rangeUpgradeTier = 0)
	{
		var loadout = ShipLoadout.Create(
			FighterSpec.Instance,
			FighterSpec.Instance.DefaultBaseHullPoints,
			FighterSpec.Instance.DefaultBaseShieldPoints,
			[
				new InstalledAbility(
					EAbilityKind.LightningCannon,
					ESpatialOrientation.Forward,
					damageUpgradeTier,
					rangeUpgradeTier),
			]);
		return ShipInstance.FromSpec(id, FighterSpec.Instance, loadout);
	}

	[Fact]
	public void TryWithUpgradedBaseShields_IncreasesBaseAndCurrentShields()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var previousMax = ship.Loadout.BaseShieldPoints[ESpatialOrientation.Forward];

		Assert.True(ship.TryWithUpgradedBaseShields(ESpatialOrientation.Forward, out var after));

		Assert.Equal(1, after.Loadout.ShieldUpgradeTiers[ESpatialOrientation.Forward]);
		Assert.Equal(previousMax + 100, after.Loadout.BaseShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(previousMax + 100, after.CurrentShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(ship.Loadout.BaseShieldPoints[ESpatialOrientation.Port], after.Loadout.BaseShieldPoints[ESpatialOrientation.Port]);
		Assert.Equal(ship.CurrentShieldPoints[ESpatialOrientation.Port], after.CurrentShieldPoints[ESpatialOrientation.Port]);
	}

	[Fact]
	public void TryWithUpgradedBaseShields_WhenCurrentBelowBase_FillsUpgradedFace()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var previousMax = ship.Loadout.BaseShieldPoints[ESpatialOrientation.Forward];
		ship.CurrentShieldPoints[ESpatialOrientation.Forward] = 0;
		var previousPort = ship.CurrentShieldPoints[ESpatialOrientation.Port];

		Assert.True(ship.TryWithUpgradedBaseShields(ESpatialOrientation.Forward, out var after));

		Assert.Equal(previousMax + 100, after.Loadout.BaseShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(after.Loadout.BaseShieldPoints[ESpatialOrientation.Forward], after.CurrentShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(previousPort, after.CurrentShieldPoints[ESpatialOrientation.Port]);
	}

	[Fact]
	public void TryWithUpgradedBaseShields_ZeroBaseFaceBecomesShieldedAndStopsAtPerFaceCap()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var face = ESpatialOrientation.Dorsal;
		ship.Loadout.BaseShieldPoints[face] = 0;
		ship.CurrentShieldPoints[face] = 0;

		for (var tier = 1; tier <= ShipLoadout.MaxShieldUpgradeTier; tier++)
		{
			Assert.True(ship.TryWithUpgradedBaseShields(face, out var after));
			Assert.Equal(tier, after.Loadout.ShieldUpgradeTiers[face]);
			Assert.Equal(tier * 100, after.Loadout.BaseShieldPoints[face]);
			Assert.Equal(tier * 100, after.CurrentShieldPoints[face]);
			ship = after;
		}

		Assert.False(ship.TryWithUpgradedBaseShields(face, out _));
		Assert.True(ship.TryWithUpgradedBaseShields(ESpatialOrientation.Forward, out _));
	}

	[Fact]
	public void ShieldUpgradeTiers_AreIndependentAcrossCopiesAndValidateBounds()
	{
		var loadout = ShipCatalog.NewRunLoadoutFor(EType.Fighter);
		var tiers = new GrimSpace.Units.Loadouts.Defenses.FaceShieldPoints();
		tiers[ESpatialOrientation.Port] = 2;
		var upgraded = ShipLoadout.Create(
			FighterSpec.Instance,
			loadout.BaseHullPoints,
			loadout.BaseShieldPoints,
			loadout.InstalledAbilities,
			tiers);
		tiers[ESpatialOrientation.Port] = 0;
		var copy = upgraded.DeepCopy();
		copy.ShieldUpgradeTiers[ESpatialOrientation.Port] = 1;

		Assert.Equal(2, upgraded.ShieldUpgradeTiers[ESpatialOrientation.Port]);
		Assert.Equal(1, copy.ShieldUpgradeTiers[ESpatialOrientation.Port]);
		tiers[ESpatialOrientation.Port] = -1;
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			ShipLoadout.Create(FighterSpec.Instance, loadout.BaseHullPoints, loadout.BaseShieldPoints, loadout.InstalledAbilities, tiers));
		tiers[ESpatialOrientation.Port] = ShipLoadout.MaxShieldUpgradeTier + 1;
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			ShipLoadout.Create(FighterSpec.Instance, loadout.BaseHullPoints, loadout.BaseShieldPoints, loadout.InstalledAbilities, tiers));
	}

	[Fact]
	public void TryWithDamageUpgraded_ReplacesOnlyTargetMountAndBumpsDamageTier()
	{
		var ship = FullCatalogFighter("fighter-1");
		var mount = new AbilityMount(EAbilityKind.ScrapDroneSwarm, ESpatialOrientation.Port);

		Assert.True(ship.TryWithDamageUpgraded(mount, out var after));

		var port = after.Loadout.InstalledAbilities.First(a => a.MountedOn == ESpatialOrientation.Port);
		Assert.IsType<ScrapDroneSwarmSpec>(port.Spec);
		Assert.Equal((2 * 100), ((ScrapDroneSwarmSpec)port.Spec).Damage);
		Assert.Equal(ScrapDroneSwarmSpec.Baseline.Heal, ((ScrapDroneSwarmSpec)port.Spec).Heal);
		Assert.Equal(1, ((ScrapDroneSwarmSpec)port.Spec).DamageUpgradeTier);

		var starboard = after.Loadout.InstalledAbilities.First(a => a.MountedOn == ESpatialOrientation.Starboard);
		Assert.Equal(0, ((ScrapDroneSwarmSpec)starboard.Spec).DamageUpgradeTier);
	}

	[Fact]
	public void TryWithRangeUpgraded_IncreasesScrapDroneSwarmBurstRange()
	{
		var ship = FullCatalogFighter("fighter-1");
		var mount = new AbilityMount(EAbilityKind.ScrapDroneSwarm, ESpatialOrientation.Port);
		var beforeRange = ((ScrapDroneSwarmSpec)ship.Loadout.InstalledAbilities.First(a => a.Mount == mount).Spec).BurstRange;

		Assert.True(ship.TryWithRangeUpgraded(mount, out var after));

		var swarm = (ScrapDroneSwarmSpec)after.Loadout.InstalledAbilities.First(a => a.Mount == mount).Spec;
		Assert.Equal(beforeRange + 1, swarm.BurstRange);
		Assert.Equal(1, swarm.RangeUpgradeTier);
	}

	[Fact]
	public void TryWithDamageUpgraded_StopsAtMaxTier()
	{
		var ship = FullCatalogFighter("fighter-1");
		var mount = new AbilityMount(EAbilityKind.ScrapDroneSwarm, ESpatialOrientation.Port);

		for (var tier = 0; tier < ScrapDroneSwarmSpec.MaxDamageUpgradeTier; tier++)
			Assert.True(ship.TryWithDamageUpgraded(mount, out ship));

		Assert.False(ship.TryWithDamageUpgraded(mount, out _));
	}

	[Fact]
	public void TryWithInstalledAbility_AddsMountWhenFacetIsOpen()
	{
		var mount = new AbilityMount(EAbilityKind.VoidBombLauncher, ESpatialOrientation.Ventral);
		var loadout = ShipLoadout.Create(
			FighterSpec.Instance,
			FighterSpec.Instance.DefaultBaseHullPoints,
			FighterSpec.Instance.DefaultBaseShieldPoints,
			[
				new InstalledAbility(
					EAbilityKind.LightningCannon,
					ESpatialOrientation.Forward),
			]);
		var ship = ShipInstance.FromSpec("fighter-partial", FighterSpec.Instance, loadout);
		var installed = new InstalledAbility(mount.Kind, ESpatialOrientation.Ventral);

		Assert.True(ship.TryWithInstalledAbility(installed, out var after));
		Assert.Contains(after.Loadout.InstalledAbilities, a => a.Mount == installed.Mount);
	}

	[Fact]
	public void TryWithInstalledAbility_RejectsOccupiedFacet()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var mount = new AbilityMount(EAbilityKind.VoidBombLauncher, ESpatialOrientation.Ventral);
		var installed = new InstalledAbility(mount.Kind, ESpatialOrientation.Ventral);

		Assert.False(ship.TryWithInstalledAbility(installed, out _));
	}

	[Fact]
	public void TryWithUpgradedBaseHull_IncreasesAndFillsHullCapacity()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		ship.CurrentHullPoints = (1 * 100);
		var previousMax = ship.Loadout.BaseHullPoints;

		Assert.True(ship.TryWithUpgradedBaseHull(out var after));

		Assert.Equal(1, after.Loadout.HullUpgradeTier);
		Assert.Equal(previousMax + 100, after.Loadout.BaseHullPoints);
		Assert.Equal(after.Loadout.BaseHullPoints, after.CurrentHullPoints);
	}

	[Fact]
	public void TryWithUpgradedBaseHull_StopsAtMaxTier()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);

		for (var tier = 0; tier < ShipLoadout.MaxHullUpgradeTier; tier++)
			Assert.True(ship.TryWithUpgradedBaseHull(out ship));

		Assert.False(ship.TryWithUpgradedBaseHull(out _));
	}

	[Fact]
	public void TryWithDamageUpgradeTier_SkipsIntermediateTiers()
	{
		var baseline = LightningCannonFighter("baseline");
		var mount = new AbilityMount(EAbilityKind.LightningCannon, ESpatialOrientation.Forward);

		Assert.True(baseline.TryWithDamageUpgradeTier(mount, 2, out var fromBaseline));
		Assert.Equal(2, fromBaseline.Loadout.InstalledAbilities.Single().DamageUpgradeTier);
		Assert.Equal((4 * 100), ((LightningCannonSpec)fromBaseline.Loadout.InstalledAbilities.Single().Spec).Damage);

		var atTierOne = LightningCannonFighter("tier-one", damageUpgradeTier: 1);
		Assert.True(atTierOne.TryWithDamageUpgradeTier(mount, 2, out var fromTierOne));
		Assert.Equal(2, fromTierOne.Loadout.InstalledAbilities.Single().DamageUpgradeTier);
		Assert.Equal((4 * 100), ((LightningCannonSpec)fromTierOne.Loadout.InstalledAbilities.Single().Spec).Damage);
	}

	[Fact]
	public void TryWithRangeUpgradeTier_SkipsIntermediateTiers()
	{
		var mount = new AbilityMount(EAbilityKind.LightningCannon, ESpatialOrientation.Forward);
		var baseline = LightningCannonFighter("baseline");

		Assert.True(baseline.TryWithRangeUpgradeTier(mount, 2, out var fromBaseline));
		Assert.Equal(2, fromBaseline.Loadout.InstalledAbilities.Single().RangeUpgradeTier);
		Assert.Equal(9, ((LightningCannonSpec)fromBaseline.Loadout.InstalledAbilities.Single().Spec).LineLength);

		var atTierOne = LightningCannonFighter("tier-one", rangeUpgradeTier: 1);
		Assert.True(atTierOne.TryWithRangeUpgradeTier(mount, 2, out var fromTierOne));
		Assert.Equal(2, fromTierOne.Loadout.InstalledAbilities.Single().RangeUpgradeTier);
	}

	[Fact]
	public void TryWithMaxHullUpgradeTier_AppliesFullCapacityJump()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var previousMax = ship.Loadout.BaseHullPoints;

		Assert.True(ship.TryWithMaxHullUpgradeTier(2, out var after));

		Assert.Equal(2, after.Loadout.HullUpgradeTier);
		Assert.Equal(previousMax + 2 * 100, after.Loadout.BaseHullPoints);
		Assert.Equal(after.Loadout.BaseHullPoints, after.CurrentHullPoints);
	}

	[Fact]
	public void TryWithMaxShieldUpgradeTier_AppliesFullCapacityJumpOnRequestedFace()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var face = ESpatialOrientation.Forward;
		var previousMax = ship.Loadout.BaseShieldPoints[face];
		var previousPort = ship.Loadout.BaseShieldPoints[ESpatialOrientation.Port];

		Assert.True(ship.TryWithMaxShieldUpgradeTier(face, 2, out var after));

		Assert.Equal(2, after.Loadout.ShieldUpgradeTiers[face]);
		Assert.Equal(previousMax + 2 * 100, after.Loadout.BaseShieldPoints[face]);
		Assert.Equal(after.Loadout.BaseShieldPoints[face], after.CurrentShieldPoints[face]);
		Assert.Equal(previousPort, after.Loadout.BaseShieldPoints[ESpatialOrientation.Port]);
	}

	[Fact]
	public void TryWithMaxShieldPoints_SetsRawFaceMaximum()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var face = ESpatialOrientation.Retro;
		Assert.Equal(0, ship.Loadout.BaseShieldPoints[face]);

		Assert.True(ship.TryWithMaxShieldPoints(face, (2 * 100), out var after));

		Assert.Equal((2 * 100), after.Loadout.BaseShieldPoints[face]);
		Assert.Equal(2, after.Loadout.ShieldUpgradeTiers[face]);
		Assert.False(after.TryWithMaxShieldPoints(face, (2 * 100), out _));
	}

	[Fact]
	public void TryWithMaxShieldPoints_RejectsBaselineAlreadyAtTarget()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var face = ESpatialOrientation.Forward;
		Assert.Equal((1 * 100), ship.Loadout.BaseShieldPoints[face]);

		Assert.False(ship.TryWithMaxShieldPoints(face, (1 * 100), out _));
	}

	[Fact]
	public void TryWithTargetTierUpgrade_RejectsEqualDowngradeAndOverMax()
	{
		var mount = new AbilityMount(EAbilityKind.LightningCannon, ESpatialOrientation.Forward);
		var ship = LightningCannonFighter("fighter", damageUpgradeTier: 1, rangeUpgradeTier: 1);

		Assert.False(ship.TryWithDamageUpgradeTier(mount, 1, out _));
		Assert.False(ship.TryWithDamageUpgradeTier(mount, 0, out _));
		Assert.False(ship.TryWithDamageUpgradeTier(mount, LightningCannonSpec.MaxDamageUpgradeTier + 1, out _));
		Assert.False(ship.TryWithRangeUpgradeTier(mount, 1, out _));
		Assert.False(ship.TryWithMaxHullUpgradeTier(0, out _));
		Assert.False(ship.TryWithMaxHullUpgradeTier(ShipLoadout.MaxHullUpgradeTier + 1, out _));
		Assert.False(ship.TryWithMaxShieldUpgradeTier(ESpatialOrientation.Forward, 0, out _));
	}

	[Fact]
	public void IncrementalUpgradeApis_StillAdvanceExactlyOneTier()
	{
		var mount = new AbilityMount(EAbilityKind.LightningCannon, ESpatialOrientation.Forward);
		var ship = LightningCannonFighter("fighter");

		Assert.True(ship.TryWithDamageUpgraded(mount, out ship));
		Assert.Equal(1, ship.Loadout.InstalledAbilities.Single().DamageUpgradeTier);

		Assert.True(ship.TryWithRangeUpgraded(mount, out ship));
		Assert.Equal(1, ship.Loadout.InstalledAbilities.Single().RangeUpgradeTier);

		Assert.True(ship.TryWithUpgradedBaseHull(out ship));
		Assert.Equal(1, ship.Loadout.HullUpgradeTier);
	}
}
