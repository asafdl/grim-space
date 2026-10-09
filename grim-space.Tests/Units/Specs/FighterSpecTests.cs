using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Specs;

namespace GrimSpace.Tests.Units.Specs;

public sealed class FighterSpecTests
{
	[Fact]
	public void Slots_IncludeSevenWeaponMounts()
	{
		var spec = FighterSpec.Instance;

		Assert.Equal(7, spec.Slots.Count);
		Assert.True(spec.Supports(new AbilityMount(EAbilityKind.LightningCannon, ESpatialOrientation.Forward)));
		Assert.True(spec.Supports(new AbilityMount(EAbilityKind.GoopGun, ESpatialOrientation.Forward)));
		Assert.True(spec.Supports(new AbilityMount(EAbilityKind.ScrapDroneSwarm, ESpatialOrientation.Port)));
	}

	[Fact]
	public void AbilitySpec_DefinesBaseline()
	{
		Assert.Equal(
			ScrapDroneSwarmSpec.Baseline,
			AbilitySpec.BaselineFor(EAbilityKind.ScrapDroneSwarm));
	}

	[Fact]
	public void LightningCannonBaseline_HasStarterStats()
	{
		var lightningCannon = LightningCannonSpec.Baseline;

		Assert.Equal(2, lightningCannon.Damage);
		Assert.Equal(5, lightningCannon.LineLength);
		Assert.Equal(2, lightningCannon.PyramidRange);
	}

	[Fact]
	public void NewDefaultLoadout_MatchesRunStarter()
	{
		var fromCatalog = ShipCatalog.NewRunLoadoutFor(EType.Fighter);
		var fromSpec = FighterSpec.Instance.NewDefaultLoadout();

		Assert.Equal(fromCatalog.InstalledAbilities.Count, fromSpec.InstalledAbilities.Count);
		Assert.Equal(fromCatalog.MaxHullPoints, fromSpec.MaxHullPoints);
		Assert.True(fromCatalog.MaxShieldPoints.Matches(fromSpec.MaxShieldPoints));
	}

	[Fact]
	public void NewRunLoadout_UsesAbilityBaselines()
	{
		var loadout = ShipCatalog.NewRunLoadoutFor(EType.Fighter);

		Assert.Equal(FighterSpec.Instance.Slots.Count, loadout.InstalledAbilities.Count);
		Assert.All(loadout.InstalledAbilities, installed =>
		{
			Assert.Equal(AbilitySpec.BaselineFor(installed.Kind), installed.Spec);
			Assert.Equal(0, installed.DamageUpgradeTier);
			Assert.Equal(0, installed.RangeUpgradeTier);
		});
	}
}
