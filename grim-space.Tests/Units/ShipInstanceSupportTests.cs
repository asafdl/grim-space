using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Units;

public sealed class ShipInstanceSupportTests
{
	[Fact]
	public void TryWithUpgradedMaxShields_IncreasesMaxAndCurrentShields()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var previousMax = ship.Spec.MaxShieldPoints[ESpatialOrientation.Forward];

		Assert.True(ship.TryWithUpgradedMaxShields(out var after));

		Assert.Equal(1, after.Spec.ShieldUpgradeTier);
		Assert.Equal(previousMax + 1, after.Spec.MaxShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(previousMax + 1, after.ShieldPoints[ESpatialOrientation.Forward]);
	}

	[Fact]
	public void TryWithUpgradedAbility_ReplacesOnlyTargetMountAndBumpsUpgradeTier()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var mount = new AbilityMount(EAbilityKind.Flak, ESpatialOrientation.Port);

		Assert.True(ship.TryWithUpgradedAbility(mount, out var after));

		var port = after.Spec.InstalledAbilities.First(a => a.MountedOn == ESpatialOrientation.Port);
		Assert.IsType<FlakSpec>(port.Spec);
		Assert.Equal(2, ((FlakSpec)port.Spec).Damage);
		Assert.Equal(1, ((FlakSpec)port.Spec).UpgradeTier);

		var starboard = after.Spec.InstalledAbilities.First(a => a.MountedOn == ESpatialOrientation.Starboard);
		Assert.Equal(0, ((FlakSpec)starboard.Spec).UpgradeTier);
	}

	[Fact]
	public void TryWithUpgradedMaxShields_WhenCurrentBelowMax_RaisesCurrentWithoutExceedingNewCap()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var previousMax = ship.Spec.MaxShieldPoints[ESpatialOrientation.Forward];
		ship.ShieldPoints[ESpatialOrientation.Forward] = 0;
		ship.ShieldPoints[ESpatialOrientation.Port] = previousMax;

		Assert.True(ship.TryWithUpgradedMaxShields(out var after));

		Assert.Equal(previousMax + 1, after.Spec.MaxShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(1, after.ShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(previousMax + 1, after.ShieldPoints[ESpatialOrientation.Port]);
	}
}
