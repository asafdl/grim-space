using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Specs;

namespace GrimSpace.Tests.Units.Specs;

public sealed class IndustrialGooperSpecTests
{
	[Fact]
	public void Slots_AreForwardGoopGunAndRetroScrapDroneSwarm()
	{
		var spec = IndustrialGooperSpec.Instance;

		Assert.Equal(2, spec.Slots.Count);
		Assert.True(spec.Supports(new AbilityMount(EAbilityKind.GoopGun, ESpatialOrientation.Forward)));
		Assert.True(spec.Supports(new AbilityMount(EAbilityKind.ScrapDroneSwarm, ESpatialOrientation.Retro)));
	}

	[Fact]
	public void DefaultLoadout_IsHeavyHullAndShieldProfile()
	{
		var spec = IndustrialGooperSpec.Instance;
		var loadout = spec.NewDefaultLoadout();

		Assert.Equal(3, loadout.MaxHullPoints);
		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
			Assert.Equal(3, loadout.MaxShieldPoints[face]);
		Assert.Equal(2, loadout.InstalledAbilities.Count);
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
