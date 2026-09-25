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
		Assert.True(spec.Supports(new AbilityMount(EAbilityKind.Railgun, ESpatialOrientation.Forward)));
		Assert.True(spec.Supports(new AbilityMount(EAbilityKind.Flak, ESpatialOrientation.Port)));
	}

	[Fact]
	public void BaselineFor_ReturnsPrototypeAbilitySpecs()
	{
		var spec = FighterSpec.Instance;
		var mount = new AbilityMount(EAbilityKind.Flak, ESpatialOrientation.Starboard);

		Assert.IsType<FlakSpec>(spec.BaselineFor(mount));
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
		var railgun = (RailgunSpec)loadout.InstalledAbilities
			.Single(ability => ability.Spec.Kind == EAbilityKind.Railgun).Spec;
		var launcher = (TorpedoLauncherSpec)loadout.InstalledAbilities
			.Single(ability => ability.Spec.Kind == EAbilityKind.TorpedoLauncher).Spec;

		Assert.Equal(2, loadout.InstalledAbilities.Count);
		Assert.Equal(2, railgun.Damage);
		Assert.Equal(5, railgun.LineLength);
		Assert.Equal(2, railgun.PyramidRange);
		Assert.Equal(2, launcher.FuelTurns);
	}
}
