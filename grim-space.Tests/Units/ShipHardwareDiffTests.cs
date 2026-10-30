using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Specs;

namespace GrimSpace.Tests.Units;

public sealed class ShipHardwareDiffTests
{
	[Fact]
	public void Compare_IdenticalLoadouts_IsEmpty()
	{
		var loadout = ShipCatalog.LoadoutForTier(EType.Fighter, EShipGearTier.T0);

		var diff = ShipHardwareDiff.Compare(EType.Fighter, loadout, EType.Fighter, loadout);

		Assert.True(diff.IsEmpty);
		Assert.False(diff.ChassisChanged);
		Assert.False(diff.GearTierChanged);
		Assert.Equal(0, diff.MaxHullDelta);
		Assert.Empty(diff.ShieldDeltas);
		Assert.Empty(diff.AddedAbilities);
		Assert.Empty(diff.RemovedAbilities);
		Assert.Empty(diff.ChangedMounts);
	}

	[Fact]
	public void Compare_SameChassisHigherTier_ReportsHardwareGains()
	{
		var baseline = ShipCatalog.LoadoutForTier(EType.Fighter, EShipGearTier.T0);
		var candidate = ShipCatalog.LoadoutForTier(EType.Fighter, EShipGearTier.T2);

		var diff = ShipHardwareDiff.Compare(EType.Fighter, baseline, EType.Fighter, candidate);

		Assert.False(diff.IsEmpty);
		Assert.False(diff.ChassisChanged);
		Assert.True(diff.GearTierChanged);
		Assert.Equal(EShipGearTier.T0, diff.BaselineGearTier);
		Assert.Equal(EShipGearTier.T2, diff.CandidateGearTier);
		Assert.Equal(0, diff.InitiativeDelta);
		Assert.True(
			diff.MaxHullDelta > 0
			|| diff.ShieldDeltas.Count > 0
			|| diff.AddedAbilities.Count > 0
			|| diff.ChangedMounts.Count > 0);
	}

	[Fact]
	public void Compare_AddedAndRemovedMounts_AreReported()
	{
		var baseline = ShipLoadout.Create(
			FighterSpec.Instance,
			FighterSpec.Instance.DefaultMaxHullPoints,
			FighterSpec.Instance.DefaultMaxShieldPoints,
			[new InstalledAbility(EAbilityKind.LightningCannon, ESpatialOrientation.Forward)]);
		var candidate = ShipLoadout.Create(
			FighterSpec.Instance,
			FighterSpec.Instance.DefaultMaxHullPoints,
			FighterSpec.Instance.DefaultMaxShieldPoints,
			[
				new InstalledAbility(EAbilityKind.ScrapDroneSwarm, ESpatialOrientation.Port),
				new InstalledAbility(EAbilityKind.GoopGun, ESpatialOrientation.Forward),
			]);

		var diff = ShipHardwareDiff.Compare(EType.Fighter, baseline, EType.Fighter, candidate);

		Assert.Contains(
			diff.RemovedAbilities,
			ability => ability.Mount == new AbilityMount(EAbilityKind.LightningCannon, ESpatialOrientation.Forward));
		Assert.Contains(
			diff.AddedAbilities,
			ability => ability.Mount == new AbilityMount(EAbilityKind.ScrapDroneSwarm, ESpatialOrientation.Port));
		Assert.Contains(
			diff.AddedAbilities,
			ability => ability.Mount == new AbilityMount(EAbilityKind.GoopGun, ESpatialOrientation.Forward));
		Assert.Empty(diff.ChangedMounts);
	}

	[Fact]
	public void Compare_SharedMountUpgradeTiers_AreReported()
	{
		var baseline = ShipLoadout.Create(
			FighterSpec.Instance,
			200,
			FighterSpec.Instance.DefaultMaxShieldPoints,
			[new InstalledAbility(EAbilityKind.LightningCannon, ESpatialOrientation.Forward)]);
		var candidate = ShipLoadout.Create(
			FighterSpec.Instance,
			200,
			FighterSpec.Instance.DefaultMaxShieldPoints,
			[new InstalledAbility(EAbilityKind.LightningCannon, ESpatialOrientation.Forward, damageUpgradeTier: 2, rangeUpgradeTier: 1)]);

		var diff = ShipHardwareDiff.Compare(EType.Fighter, baseline, EType.Fighter, candidate);

		var change = Assert.Single(diff.ChangedMounts);
		Assert.Equal(new AbilityMount(EAbilityKind.LightningCannon, ESpatialOrientation.Forward), change.Mount);
		Assert.Equal(2, change.DamageTiersDelta);
		Assert.Equal(1, change.RangeTiersDelta);
	}

	[Fact]
	public void Compare_CrossChassis_ReportsChassisAndMobilityDeltas()
	{
		var fighter = ShipCatalog.LoadoutForTier(EType.Fighter, EShipGearTier.T0);
		var gunship = ShipCatalog.LoadoutForTier(EType.Gunship, EShipGearTier.T0);

		var diff = ShipHardwareDiff.Compare(EType.Fighter, fighter, EType.Gunship, gunship);

		Assert.True(diff.ChassisChanged);
		Assert.Equal(EType.Fighter, diff.BaselineChassis);
		Assert.Equal(EType.Gunship, diff.CandidateChassis);
		Assert.Equal(
			GunshipSpec.Instance.Initiative - FighterSpec.Instance.Initiative,
			diff.InitiativeDelta);
		Assert.Equal(
			GunshipSpec.Instance.Maneuverability.MaxActionPoints
				- FighterSpec.Instance.Maneuverability.MaxActionPoints,
			diff.ActionPointsDelta);
		Assert.Equal(
			GunshipSpec.Instance.Maneuverability.MaxManeuverPoints
				- FighterSpec.Instance.Maneuverability.MaxManeuverPoints,
			diff.ManeuverPointsDelta);
		Assert.Equal(
			gunship.MaxHullPoints - fighter.MaxHullPoints,
			diff.MaxHullDelta);
	}
}
