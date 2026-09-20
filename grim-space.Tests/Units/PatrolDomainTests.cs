using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Units;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.Units;

public sealed class PatrolDomainTests
{
	[Fact]
	public void PatrolStatsAreConfigured()
	{
		var stats = Stats.ForType(EType.Patrol);
		var configuration = ShipCatalog.DefaultFor(EType.Patrol);

		var maxShields = ShipCatalog.MaxShieldPointsFor(EType.Patrol);

		Assert.Equal(4, stats.MaxAp);
		Assert.Equal(1, configuration.MaxHullPoints);
		Assert.Equal(3, maxShields[GrimSpace.Math.Grid.ESpatialOrientation.Forward]);
		Assert.Equal(0, maxShields[GrimSpace.Math.Grid.ESpatialOrientation.Retro]);
		Assert.Equal(1, AbilityLoadout.PerTurnUsesForAbility(
			ShipInstance.FromCatalog("patrol", EType.Patrol),
			EAbilityKind.Flak));
		Assert.Equal(0, AbilityLoadout.PerTurnUsesForAbility(
			ShipInstance.FromCatalog("patrol", EType.Patrol),
			EAbilityKind.Railgun));
	}

	[Fact]
	public void PatrolAbilitiesAreFlakOnly()
	{
		var abilities = Capabilities.AbilitiesFor(EType.Patrol);

		Assert.Single(abilities, def => def is FlakDef);
		Assert.DoesNotContain(abilities, def => def is RailgunDef);
	}
}
