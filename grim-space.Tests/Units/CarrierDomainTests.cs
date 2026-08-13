using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Units;

public sealed class CarrierDomainTests
{
	[Fact]
	public void CarrierStatsAreConfigured()
	{
		var stats = Stats.ForType(EType.Carrier);

		Assert.Equal(3, stats.MaxAp);
		Assert.Equal(2, stats.MaxHullPoints);
		Assert.Equal(2, stats.MaxShieldPoints.MaxOnAnyFace);
		Assert.Equal(0, stats.FlaksPerTurn);
		Assert.Equal(1, stats.RailgunsPerTurn);
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
		var unit = UnitDisplayState.Capture(new State
		{
			Id = "carrier",
			Type = EType.Carrier,
			Position = new Coord(5, 5, 5),
			Fore = Coord.Forward,
			Dorsal = Coord.Up,
			Starboard = Coord.Cross(Coord.Up, Coord.Forward),
			Stats = Stats.ForType(EType.Carrier),
		});

		var ready = AbilityHudCatalog.BuildState(
			AbilityHudCatalog.ForUnit(EType.Carrier)[1],
			unit,
			new AbilityLegality(WeaponPeek.Empty, SpawnPatrol: true));
		var cooling = AbilityHudCatalog.BuildState(
			AbilityHudCatalog.ForUnit(EType.Carrier)[1],
			unit with { PatrolSpawnCooldownRemaining = CombatConfig.PatrolCooldownTurns },
			new AbilityLegality(WeaponPeek.Empty, SpawnPatrol: true));

		Assert.Equal("1/1", ready.Charges);
		Assert.Equal("0/1", cooling.Charges);
	}
}
