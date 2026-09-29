using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Specs;

namespace GrimSpace.Tests.Units.Specs;

public sealed class FighterSpecTests
{
	[Fact]
	public void Slots_IncludeSixWeaponMounts()
	{
		var spec = FighterSpec.Instance;

		Assert.Equal(6, spec.Slots.Count);
		Assert.True(spec.Supports(new AbilityMount(EAbilityKind.LightningCannon, ESpatialOrientation.Forward)));
		Assert.True(spec.Supports(new AbilityMount(EAbilityKind.ScrapDroneSwarm, ESpatialOrientation.Port)));
	}

	[Fact]
	public void BaselineFor_ReturnsPrototypeAbilitySpecs()
	{
		var spec = FighterSpec.Instance;
		var mount = new AbilityMount(EAbilityKind.ScrapDroneSwarm, ESpatialOrientation.Starboard);

		Assert.IsType<ScrapDroneSwarmSpec>(spec.BaselineFor(mount));
	}

	[Fact]
	public void NewDefaultLoadout_InstallsAllSlots()
	{
		var loadout = FighterSpec.Instance.NewDefaultLoadout();

		Assert.Equal(6, loadout.InstalledAbilities.Count);
		foreach (var slot in FighterSpec.Instance.Slots)
			Assert.Contains(loadout.InstalledAbilities, installed => installed.Mount == slot.Mount);
	}

	[Fact]
	public void NewRunLoadout_IsStarterConfiguration()
	{
		var loadout = ShipCatalog.NewRunLoadoutFor(EType.Fighter);
		var lightningCannon = (LightningCannonSpec)loadout.InstalledAbilities
			.Single(ability => ability.Spec.Kind == EAbilityKind.LightningCannon).Spec;
		var launcher = (TorpedoLauncherSpec)loadout.InstalledAbilities
			.Single(ability => ability.Spec.Kind == EAbilityKind.TorpedoLauncher).Spec;

		Assert.Equal(2, loadout.InstalledAbilities.Count);
		Assert.Equal(2, lightningCannon.Damage);
		Assert.Equal(5, lightningCannon.LineLength);
		Assert.Equal(2, lightningCannon.PyramidRange);
		Assert.Equal(2, launcher.FuelTurns);
	}
}
