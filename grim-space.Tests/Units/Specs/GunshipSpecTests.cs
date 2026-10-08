using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Specs;

namespace GrimSpace.Tests.Units.Specs;

public sealed class GunshipSpecTests
{
	[Fact]
	public void Slots_ArePortAndStarboardLongRangeLightningCannons()
	{
		var spec = GunshipSpec.Instance;

		Assert.Equal(2, spec.Slots.Count);
		Assert.True(spec.Supports(new AbilityMount(EAbilityKind.LightningCannon, ESpatialOrientation.Port)));
		Assert.True(spec.Supports(new AbilityMount(EAbilityKind.LightningCannon, ESpatialOrientation.Starboard)));

		var port = (LightningCannonSpec)spec.BaselineFor(new AbilityMount(EAbilityKind.LightningCannon, ESpatialOrientation.Port));
		Assert.Equal(9, port.LineLength);
		Assert.Equal(4, port.PyramidRange);
		Assert.Equal(3, port.Damage);
		Assert.Equal(1, port.UsesPerTurn);
	}

	[Fact]
	public void DefaultLoadout_MatchesCombatSheet()
	{
		var spec = GunshipSpec.Instance;
		var loadout = spec.NewDefaultLoadout();

		Assert.Equal(2, loadout.MaxHullPoints);
		Assert.Equal(1, loadout.MaxShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(1, loadout.MaxShieldPoints[ESpatialOrientation.Retro]);
		Assert.Equal(2, loadout.MaxShieldPoints[ESpatialOrientation.Port]);
		Assert.Equal(2, loadout.MaxShieldPoints[ESpatialOrientation.Starboard]);
		Assert.Equal(0, loadout.MaxShieldPoints[ESpatialOrientation.Dorsal]);
		Assert.Equal(0, loadout.MaxShieldPoints[ESpatialOrientation.Ventral]);
		Assert.Equal(2, loadout.InstalledAbilities.Count);
	}

	[Fact]
	public void Catalog_T0LoadoutMatchesSpecDefault()
	{
		var fromCatalog = ShipCatalog.LoadoutForTier(EType.Gunship, EShipGearTier.T0);
		var fromSpec = GunshipSpec.Instance.NewDefaultLoadout();

		Assert.True(fromCatalog.MaxShieldPoints.Matches(fromSpec.MaxShieldPoints));
		Assert.Equal(fromSpec.InstalledAbilities.Count, fromCatalog.InstalledAbilities.Count);
	}
}
