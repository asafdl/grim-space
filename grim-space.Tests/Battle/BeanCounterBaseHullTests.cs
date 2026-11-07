using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.PilotCapabilities;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.Factions;

namespace GrimSpace.Tests.PilotCapabilities;

public sealed class BeanCounterBaseHullTests
{
	[Fact]
	public void CreateBattleUnit_ScalesBaseHullAndPreservesMissing()
	{
		var ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter);
		ship.CurrentHullPoints = ship.Loadout.BaseHullPoints - 20;
		var spawn = BeanCounterSpawn(ship);

		var unit = Factory.Create(spawn);
		var expectedBase = Scale(ship.Loadout.BaseHullPoints, PilotSkillPercents.BeanCounter.BaseHullPercent);

		Assert.Equal(expectedBase, unit.State.Loadout.BaseHullPoints);
		Assert.Equal(expectedBase - 20, unit.State.CurrentHullPoints);
		Assert.Equal(ship.Loadout.BaseHullPoints, unit.State.HullHandoffBasePoints);
		Assert.Equal(ship.Loadout.BaseHullPoints, spawn.Ship.Loadout.BaseHullPoints);
	}

	[Fact]
	public void HullPointsForHandoff_RestoresUndamagedShipToPreModCurrent()
	{
		var ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter);
		var unit = Factory.Create(BeanCounterSpawn(ship));

		Assert.Equal(ship.CurrentHullPoints, unit.State.HullPointsForHandoff());
	}

	[Fact]
	public void HullPointsForHandoff_PreservesDamageTakenAgainstPreModBase()
	{
		var ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter);
		var unit = Factory.Create(BeanCounterSpawn(ship));
		unit.State.CurrentHullPoints -= 10;

		Assert.Equal(ship.CurrentHullPoints - 10, unit.State.HullPointsForHandoff());
	}

	[Fact]
	public void CreateBattleUnit_StacksBaseHullPenaltyMultiplicatively()
	{
		var ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter);
		var grant = Assert.Single(
			PilotCapabilityGrant.FromBackground(
				"pilot-a",
				EPilotBackgroundId.BeanCounter,
				id => id == EPilotCapabilityId.BaseHullPenalty));
		var spawn = new BattleSpawn
		{
			Ship = ship,
			Pilot = new PilotBattleSnapshot("pilot-a", [grant, grant]),
			Team = ETeam.Player,
			Faction = EFaction.Player,
			Position = Coord.Zero,
			ExecutionAgent = new UserExecutionAgent(),
		};

		var unit = Factory.Create(spawn);
		var once = Scale(ship.Loadout.BaseHullPoints, PilotSkillPercents.BeanCounter.BaseHullPercent);
		var twice = Scale(once, PilotSkillPercents.BeanCounter.BaseHullPercent);

		Assert.Equal(twice, unit.State.Loadout.BaseHullPoints);
		Assert.Equal(twice, unit.State.CurrentHullPoints);
	}

	private static BattleSpawn BeanCounterSpawn(ShipInstance ship)
	{
		const string pilotId = "pilot-a";
		return new BattleSpawn
		{
			Ship = ship,
			Pilot = new PilotBattleSnapshot(
				pilotId,
				PilotCapabilityGrant.FromBackground(
					pilotId,
					EPilotBackgroundId.BeanCounter,
					BattlePilotCapabilityPipelineFactory.Supports)),
			Team = ETeam.Player,
			Faction = EFaction.Player,
			Position = Coord.Zero,
			ExecutionAgent = new UserExecutionAgent(),
		};
	}

	private static int Scale(int amount, int percent) =>
		(int)System.Math.Round(amount * percent / 100m, MidpointRounding.AwayFromZero);
}
