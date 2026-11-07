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

public sealed class ShieldMasterBaseShieldTests
{
	[Fact]
	public void CreateBattleUnit_IncreasesBaseShieldsPerFaceAndPreservesMissing()
	{
		var ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter);
		ship.CurrentShieldPoints[ESpatialOrientation.Forward] =
			ship.Loadout.BaseShieldPoints[ESpatialOrientation.Forward] - 20;
		var spawn = ShieldMasterSpawn(ship);

		var unit = Factory.Create(spawn);
		var bonus = PilotSkillPercents.ShieldMaster.BaseShieldBonusPerFace;

		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
		{
			var expectedBase = ship.Loadout.BaseShieldPoints[face] + bonus;
			var missing = System.Math.Max(
				0,
				ship.Loadout.BaseShieldPoints[face] - ship.CurrentShieldPoints[face]);
			Assert.Equal(expectedBase, unit.State.Loadout.BaseShieldPoints[face]);
			Assert.Equal(expectedBase - missing, unit.State.CurrentShieldPoints[face]);
			Assert.Equal(
				ship.Loadout.BaseShieldPoints[face],
				unit.State.ShieldHandoffBasePoints![face]);
			Assert.Equal(ship.Loadout.BaseShieldPoints[face], spawn.Ship.Loadout.BaseShieldPoints[face]);
		}
	}

	[Fact]
	public void ShieldPointsForHandoff_RestoresUndamagedShipToPreModCurrent()
	{
		var ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter);
		var unit = Factory.Create(ShieldMasterSpawn(ship));

		Assert.True(ship.CurrentShieldPoints.Matches(unit.State.ShieldPointsForHandoff()));
	}

	[Fact]
	public void CreateBattleUnit_StacksBaseShieldBonusAdditively()
	{
		var ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter);
		var grant = Assert.Single(
			PilotCapabilityGrant.FromQuirks(
				"pilot-a",
				[EPilotQuirkId.ShieldMaster],
				id => id == EPilotCapabilityId.BaseShieldBonus));
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
		var bonus = 2 * PilotSkillPercents.ShieldMaster.BaseShieldBonusPerFace;

		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
			Assert.Equal(
				ship.Loadout.BaseShieldPoints[face] + bonus,
				unit.State.Loadout.BaseShieldPoints[face]);
	}

	[Fact]
	public void CreateBattleUnit_NetsBonusAndPenaltyAgainstPreModBase()
	{
		var ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter);
		var bonusGrant = Assert.Single(
			PilotCapabilityGrant.FromQuirks(
				"pilot-a",
				[EPilotQuirkId.ShieldMaster],
				id => id == EPilotCapabilityId.BaseShieldBonus));
		var penaltyGrant = Assert.Single(
			PilotCapabilityGrant.FromQuirks(
				"pilot-a",
				[EPilotQuirkId.BadTechnician],
				id => id == EPilotCapabilityId.BaseShieldPenalty));
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
			PilotSkillPercents.ShieldMaster.BaseShieldBonusPerFace
			- PilotSkillPercents.BadTechnician.BaseShieldPenaltyPerFace;

		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
		{
			Assert.Equal(
				ship.Loadout.BaseShieldPoints[face] + delta,
				unit.State.Loadout.BaseShieldPoints[face]);
			Assert.Equal(
				ship.Loadout.BaseShieldPoints[face],
				unit.State.ShieldHandoffBasePoints![face]);
		}
	}

	private static BattleSpawn ShieldMasterSpawn(ShipInstance ship)
	{
		const string pilotId = "pilot-a";
		return new BattleSpawn
		{
			Ship = ship,
			Pilot = new PilotBattleSnapshot(
				pilotId,
				PilotCapabilityGrant.FromQuirks(
					pilotId,
					[EPilotQuirkId.ShieldMaster],
					BattlePilotCapabilityPipelineFactory.Supports)),
			Team = ETeam.Player,
			Faction = EFaction.Player,
			Position = Coord.Zero,
			ExecutionAgent = new UserExecutionAgent(),
		};
	}
}
