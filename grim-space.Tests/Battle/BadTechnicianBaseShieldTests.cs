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

public sealed class BadTechnicianBaseShieldTests
{
	[Fact]
	public void CreateBattleUnit_ReducesBaseShieldsPerFaceAndPreservesMissing()
	{
		var ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter);
		ship.CurrentShieldPoints[ESpatialOrientation.Forward] =
			ship.Loadout.BaseShieldPoints[ESpatialOrientation.Forward] - 20;
		var spawn = BadTechnicianSpawn(ship);

		var unit = Factory.Create(spawn);
		var penalty = PilotSkillPercents.BadTechnician.BaseShieldPenaltyPerFace;

		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
		{
			var expectedBase = System.Math.Max(0, ship.Loadout.BaseShieldPoints[face] - penalty);
			var missing = System.Math.Max(
				0,
				ship.Loadout.BaseShieldPoints[face] - ship.CurrentShieldPoints[face]);
			Assert.Equal(expectedBase, unit.State.Loadout.BaseShieldPoints[face]);
			Assert.Equal(
				System.Math.Max(0, expectedBase - missing),
				unit.State.CurrentShieldPoints[face]);
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
		var unit = Factory.Create(BadTechnicianSpawn(ship));

		Assert.True(ship.CurrentShieldPoints.Matches(unit.State.ShieldPointsForHandoff()));
	}

	[Fact]
	public void ShieldPointsForHandoff_PreservesDamageTakenAgainstPreModBase()
	{
		var ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter);
		var unit = Factory.Create(BadTechnicianSpawn(ship));
		unit.State.CurrentShieldPoints[ESpatialOrientation.Forward] -= 10;

		var handoff = unit.State.ShieldPointsForHandoff();
		Assert.Equal(
			ship.CurrentShieldPoints[ESpatialOrientation.Forward] - 10,
			handoff[ESpatialOrientation.Forward]);
		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
		{
			if (face == ESpatialOrientation.Forward)
				continue;
			Assert.Equal(ship.CurrentShieldPoints[face], handoff[face]);
		}
	}

	[Fact]
	public void CreateBattleUnit_StacksBaseShieldPenaltyAdditively()
	{
		var ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter);
		var grant = Assert.Single(
			PilotCapabilityGrant.FromQuirks(
				"pilot-a",
				[EPilotQuirkId.BadTechnician],
				id => id == EPilotCapabilityId.BaseShieldPenalty));
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
		var penalty = 2 * PilotSkillPercents.BadTechnician.BaseShieldPenaltyPerFace;

		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
		{
			Assert.Equal(
				System.Math.Max(0, ship.Loadout.BaseShieldPoints[face] - penalty),
				unit.State.Loadout.BaseShieldPoints[face]);
		}
	}

	[Fact]
	public void CreateBattleUnit_ClampsFaceBaseAtZero()
	{
		var ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter);
		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
		{
			ship.Loadout.BaseShieldPoints[face] = 4;
			ship.CurrentShieldPoints[face] = 4;
		}

		var unit = Factory.Create(BadTechnicianSpawn(ship));

		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
		{
			Assert.Equal(0, unit.State.Loadout.BaseShieldPoints[face]);
			Assert.Equal(0, unit.State.CurrentShieldPoints[face]);
		}
	}

	private static BattleSpawn BadTechnicianSpawn(ShipInstance ship)
	{
		const string pilotId = "pilot-a";
		return new BattleSpawn
		{
			Ship = ship,
			Pilot = new PilotBattleSnapshot(
				pilotId,
				PilotCapabilityGrant.FromQuirks(
					pilotId,
					[EPilotQuirkId.BadTechnician],
					BattlePilotCapabilityPipelineFactory.Supports)),
			Team = ETeam.Player,
			Faction = EFaction.Player,
			Position = Coord.Zero,
			ExecutionAgent = new UserExecutionAgent(),
		};
	}
}
