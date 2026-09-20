using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.Units;

public sealed class CarrierDomainTests
{
	[Fact]
	public void CarrierStatsAreConfigured()
	{
		var stats = Stats.ForType(EType.Carrier);
		var configuration = ShipCatalog.DefaultFor(EType.Carrier);
		var snapshot = ShipInstance.FromCatalog("carrier", EType.Carrier);

		Assert.Equal(3, stats.MaxAp);
		Assert.Equal(2, configuration.MaxHullPoints);
		Assert.Equal(2, ShipCatalog.MaxShieldPointsFor(EType.Carrier).MaxOnAnyFace);
		Assert.Equal(0, AbilityLoadout.PerTurnUsesForAbility(snapshot, EAbilityKind.Flak));
		Assert.Equal(1, AbilityLoadout.PerTurnUsesForAbility(snapshot, EAbilityKind.Railgun));
	}

	[Fact]
	public void CarrierAbilitiesIncludeRailgunAndSpawnPatrol()
	{
		var abilities = Capabilities.AbilitiesFor(EType.Carrier);

		Assert.Contains(abilities, def => def is RailgunDef);
		Assert.Contains(abilities, def => def is SpawnPatrolDef);
		Assert.DoesNotContain(abilities, def => def is FlakDef);
	}

	[Fact]
	public void SpawnPatrolChargesShowOneWhenReady()
	{
		var unit = UnitDisplayState.Capture(
			State.FromShipInstance(
				ShipInstance.FromCatalog("carrier", EType.Carrier),
				new Coord(5, 5, 5)));

		var ready = AbilityHudCatalog.BuildState(
			AbilityHudCatalog.ForUnit(EType.Carrier)[1],
			unit,
			new AbilityLegality(WeaponPeek.Empty, SpawnPatrol: true, Detonate: false));
		var cooling = AbilityHudCatalog.BuildState(
			AbilityHudCatalog.ForUnit(EType.Carrier)[1],
			unit with
			{
				Mounts = unit.Mounts
					.Select(mount => mount.Kind == EAbilityKind.PatrolBay
						? mount with
						{
							CooldownRemaining = CatalogExpectations.DefaultPatrolBaySpec().CooldownTurns,
						}
						: mount)
					.ToList(),
			},
			new AbilityLegality(WeaponPeek.Empty, SpawnPatrol: true, Detonate: false));

		Assert.Equal("1/1", ready.Charges);
		Assert.Equal("0/1", cooling.Charges);
	}
}
