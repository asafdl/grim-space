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

public sealed class MetalMasterBaseHullTests
{
	[Fact]
	public void CreateBattleUnit_IncreasesBaseHullAndPreservesMissing()
	{
		var ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter);
		ship.CurrentHullPoints = ship.Loadout.BaseHullPoints - 20;
		var spawn = MetalMasterSpawn(ship);

		var unit = Factory.Create(spawn);
		var expectedBase = ship.Loadout.BaseHullPoints + PilotSkillPercents.MetalMaster.BaseHullBonus;

		Assert.Equal(expectedBase, unit.State.Loadout.BaseHullPoints);
		Assert.Equal(expectedBase - 20, unit.State.CurrentHullPoints);
		Assert.Equal(ship.Loadout.BaseHullPoints, unit.State.HullHandoffBasePoints);
		Assert.Equal(ship.Loadout.BaseHullPoints, spawn.Ship.Loadout.BaseHullPoints);
	}

	[Fact]
	public void HullPointsForHandoff_RestoresUndamagedShipToPreModCurrent()
	{
		var ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter);
		var unit = Factory.Create(MetalMasterSpawn(ship));

		Assert.Equal(ship.CurrentHullPoints, unit.State.HullPointsForHandoff());
	}

	[Fact]
	public void CreateBattleUnit_StacksBaseHullBonusAdditively()
	{
		var ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter);
		var grant = Assert.Single(
			PilotCapabilityGrant.FromQuirks(
				"pilot-a",
				[EPilotQuirkId.MetalMaster],
				id => id == EPilotCapabilityId.BaseHullBonus));
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
		var expectedBase = ship.Loadout.BaseHullPoints + 2 * PilotSkillPercents.MetalMaster.BaseHullBonus;

		Assert.Equal(expectedBase, unit.State.Loadout.BaseHullPoints);
		Assert.Equal(expectedBase, unit.State.CurrentHullPoints);
	}

	[Fact]
	public void CreateBattleUnit_AppliesBonusAfterBeanCounterAgainstPreModHandoff()
	{
		var ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter);
		var bonusGrant = Assert.Single(
			PilotCapabilityGrant.FromQuirks(
				"pilot-a",
				[EPilotQuirkId.MetalMaster],
				id => id == EPilotCapabilityId.BaseHullBonus));
		var penaltyGrant = Assert.Single(
			PilotCapabilityGrant.FromBackground(
				"pilot-a",
				EPilotBackgroundId.BeanCounter,
				id => id == EPilotCapabilityId.BaseHullPenalty));
		var spawn = new BattleSpawn
		{
			Ship = ship,
			Pilot = new PilotBattleSnapshot("pilot-a", [bonusGrant, penaltyGrant]),
			Team = ETeam.Player,
			Faction = EFaction.Player,
			Position = Coord.Zero,
			ExecutionAgent = new UserExecutionAgent(),
		};

		var unit = Factory.Create(spawn);
		var afterPenalty = Scale(ship.Loadout.BaseHullPoints, PilotSkillPercents.BeanCounter.BaseHullPercent);
		var expectedBase = afterPenalty + PilotSkillPercents.MetalMaster.BaseHullBonus;

		Assert.Equal(expectedBase, unit.State.Loadout.BaseHullPoints);
		Assert.Equal(ship.Loadout.BaseHullPoints, unit.State.HullHandoffBasePoints);
	}

	private static BattleSpawn MetalMasterSpawn(ShipInstance ship)
	{
		const string pilotId = "pilot-a";
		return new BattleSpawn
		{
			Ship = ship,
			Pilot = new PilotBattleSnapshot(
				pilotId,
				PilotCapabilityGrant.FromQuirks(
					pilotId,
					[EPilotQuirkId.MetalMaster],
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
