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

public sealed class WeakWeldingBaseHullTests
{
	[Fact]
	public void CreateBattleUnit_ReducesBaseHullAndPreservesMissing()
	{
		var ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter);
		ship.CurrentHullPoints = ship.Loadout.BaseHullPoints - 20;
		var spawn = WeakWeldingSpawn(ship);

		var unit = Factory.Create(spawn);
		var expectedBase = ship.Loadout.BaseHullPoints - PilotSkillPercents.WeakWelding.BaseHullPenalty;

		Assert.Equal(expectedBase, unit.State.Loadout.BaseHullPoints);
		Assert.Equal(expectedBase - 20, unit.State.CurrentHullPoints);
		Assert.Equal(ship.Loadout.BaseHullPoints, unit.State.HullHandoffBasePoints);
		Assert.Equal(ship.Loadout.BaseHullPoints, spawn.Ship.Loadout.BaseHullPoints);
	}

	[Fact]
	public void HullPointsForHandoff_RestoresUndamagedShipToPreModCurrent()
	{
		var ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter);
		var unit = Factory.Create(WeakWeldingSpawn(ship));

		Assert.Equal(ship.CurrentHullPoints, unit.State.HullPointsForHandoff());
	}

	[Fact]
	public void CreateBattleUnit_StacksFlatPenaltyAdditively()
	{
		var ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter);
		var grant = Assert.Single(
			PilotCapabilityGrant.FromQuirks(
				"pilot-a",
				[EPilotQuirkId.WeakWelding],
				id => id == EPilotCapabilityId.BaseHullFlatPenalty));
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
		var expectedBase = ship.Loadout.BaseHullPoints - 2 * PilotSkillPercents.WeakWelding.BaseHullPenalty;

		Assert.Equal(expectedBase, unit.State.Loadout.BaseHullPoints);
	}

	[Fact]
	public void CreateBattleUnit_ClampsBaseHullAtZero()
	{
		var ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter);
		ship.CurrentHullPoints = 8;
		ship.Loadout = ship.Loadout with { BaseHullPoints = 8 };

		var unit = Factory.Create(WeakWeldingSpawn(ship));

		Assert.Equal(0, unit.State.Loadout.BaseHullPoints);
		Assert.Equal(0, unit.State.CurrentHullPoints);
	}

	[Fact]
	public void CreateBattleUnit_NetsWithMetalMasterAgainstPreModHandoff()
	{
		var ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter);
		var bonusGrant = Assert.Single(
			PilotCapabilityGrant.FromQuirks(
				"pilot-a",
				[EPilotQuirkId.MetalMaster],
				id => id == EPilotCapabilityId.BaseHullBonus));
		var penaltyGrant = Assert.Single(
			PilotCapabilityGrant.FromQuirks(
				"pilot-a",
				[EPilotQuirkId.WeakWelding],
				id => id == EPilotCapabilityId.BaseHullFlatPenalty));
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
		var delta =
			PilotSkillPercents.MetalMaster.BaseHullBonus
			- PilotSkillPercents.WeakWelding.BaseHullPenalty;

		Assert.Equal(ship.Loadout.BaseHullPoints + delta, unit.State.Loadout.BaseHullPoints);
		Assert.Equal(ship.Loadout.BaseHullPoints, unit.State.HullHandoffBasePoints);
	}

	private static BattleSpawn WeakWeldingSpawn(ShipInstance ship)
	{
		const string pilotId = "pilot-a";
		return new BattleSpawn
		{
			Ship = ship,
			Pilot = new PilotBattleSnapshot(
				pilotId,
				PilotCapabilityGrant.FromQuirks(
					pilotId,
					[EPilotQuirkId.WeakWelding],
					BattlePilotCapabilityPipelineFactory.Supports)),
			Team = ETeam.Player,
			Faction = EFaction.Player,
			Position = Coord.Zero,
			ExecutionAgent = new UserExecutionAgent(),
		};
	}
}
