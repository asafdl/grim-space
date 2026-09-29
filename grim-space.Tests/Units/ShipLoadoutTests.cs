using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Specs;

namespace GrimSpace.Tests.Units;

public sealed class ShipLoadoutTests
{
	[Fact]
	public void Create_RejectsInstalledMountNotOnChassis()
	{
		var swarm = new ScrapDroneSwarmSpec(UsesPerTurn: 1, Damage: 1, BurstRange: 2);
		var installed = new[] { new InstalledAbility(swarm, ESpatialOrientation.Forward) };

		Assert.Throws<ArgumentException>(() =>
			ShipLoadout.Create(
				FighterSpec.Instance,
				2,
				FighterSpec.Instance.DefaultMaxShieldPoints,
				installed));
	}

	[Fact]
	public void Create_AllowsPartialLoadoutWithinSlotTable()
	{
		var lightningCannonMount = new AbilityMount(EAbilityKind.LightningCannon, ESpatialOrientation.Forward);
		var baseline = FighterSpec.Instance.BaselineFor(lightningCannonMount);
		var loadout = ShipLoadout.Create(
			FighterSpec.Instance,
			FighterSpec.Instance.DefaultMaxHullPoints,
			FighterSpec.Instance.DefaultMaxShieldPoints,
			[new InstalledAbility(baseline, ESpatialOrientation.Forward)]);

		Assert.Single(loadout.InstalledAbilities);
		Assert.True(FighterSpec.Instance.Supports(lightningCannonMount));
		Assert.DoesNotContain(
			loadout.InstalledAbilities,
			a => a.Mount.Kind == EAbilityKind.ScrapDroneSwarm);
	}

	[Fact]
	public void FromSpec_RejectsLoadoutIncompatibleWithChassis()
	{
		var fighterLoadout = ShipCatalog.NewRunLoadoutFor(EType.Fighter);

		Assert.Throws<ArgumentException>(() =>
			ShipInstance.FromSpec("mismatch", PatrolSpec.Instance, fighterLoadout));
	}
}
