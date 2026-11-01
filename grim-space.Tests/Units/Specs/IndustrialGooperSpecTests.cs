using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Specs;

namespace GrimSpace.Tests.Units.Specs;

public sealed class IndustrialGooperSpecTests
{
	[Fact]
	public void Slots_AreRetroGoopGunOnly()
	{
		var spec = IndustrialGooperSpec.Instance;

		Assert.Equal(EOperatorClassId.Tank, spec.ClassId);
		Assert.Equal(
			[new AbilityMount(EAbilityKind.GoopGun, ESpatialOrientation.Retro)],
			spec.Slots.Select(slot => slot.Mount).ToArray());
	}

	[Fact]
	public void DefaultLoadout_IsHeavyHullAndShieldProfile()
	{
		var spec = IndustrialGooperSpec.Instance;
		var loadout = spec.NewDefaultLoadout();

		Assert.Equal((3 * 100), loadout.MaxHullPoints);
		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
			Assert.Equal((3 * 100), loadout.MaxShieldPoints[face]);
		Assert.Equal(
			[new AbilityMount(EAbilityKind.GoopGun, ESpatialOrientation.Retro)],
			loadout.InstalledAbilities.Select(ability => ability.Mount).ToArray());
	}

	[Fact]
	public void Catalog_T0LoadoutMatchesSpecDefault()
	{
		var fromCatalog = ShipCatalog.LoadoutForTier(EType.IndustrialGooper, EShipGearTier.T0);
		var fromSpec = IndustrialGooperSpec.Instance.NewDefaultLoadout();

		Assert.True(fromCatalog.MaxShieldPoints.Matches(fromSpec.MaxShieldPoints));
		Assert.Equal(fromSpec.InstalledAbilities.Count, fromCatalog.InstalledAbilities.Count);
	}
}
