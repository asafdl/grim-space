using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Specs;

namespace GrimSpace.Tests.Units.Specs;

public sealed class GunshipSpecTests
{
	[Fact]
	public void Slots_ArePortAndStarboardBaselineLightningCannons()
	{
		var spec = GunshipSpec.Instance;

		Assert.Equal(2, spec.Slots.Count);
		Assert.True(spec.Supports(new AbilityMount(EAbilityKind.LightningCannon, ESpatialOrientation.Port)));
		Assert.True(spec.Supports(new AbilityMount(EAbilityKind.LightningCannon, ESpatialOrientation.Starboard)));

		var port = spec.NewDefaultLoadout().InstalledAbilities.Single(
			ability => ability.MountedOn == ESpatialOrientation.Port);
		Assert.Equal(LightningCannonSpec.Baseline, port.Spec);
	}

	[Fact]
	public void DefaultLoadout_MatchesCombatSheet()
	{
		var spec = GunshipSpec.Instance;
		var loadout = spec.NewDefaultLoadout();

		Assert.Equal((2 * 100), loadout.BaseHullPoints);
		Assert.Equal((1 * 100), loadout.BaseShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal((1 * 100), loadout.BaseShieldPoints[ESpatialOrientation.Retro]);
		Assert.Equal((2 * 100), loadout.BaseShieldPoints[ESpatialOrientation.Port]);
		Assert.Equal((2 * 100), loadout.BaseShieldPoints[ESpatialOrientation.Starboard]);
		Assert.Equal(0, loadout.BaseShieldPoints[ESpatialOrientation.Dorsal]);
		Assert.Equal(0, loadout.BaseShieldPoints[ESpatialOrientation.Ventral]);
		Assert.Equal(2, loadout.InstalledAbilities.Count);
	}

	[Fact]
	public void Catalog_T0LoadoutMatchesSpecDefault()
	{
		var fromCatalog = ShipCatalog.LoadoutForTier(EType.Gunship, EShipGearTier.T0);
		var fromSpec = GunshipSpec.Instance.NewDefaultLoadout();

		Assert.True(fromCatalog.BaseShieldPoints.Matches(fromSpec.BaseShieldPoints));
		Assert.Equal(fromSpec.InstalledAbilities.Count, fromCatalog.InstalledAbilities.Count);
	}
}
