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

public sealed class CyberJunkieHeatClearTests
{
	[Fact]
	public void CreateBattleUnit_AppliesHeatClearBonusAdditively()
	{
		var pilotId = "pilot-a";
		var spawn = new BattleSpawn
		{
			Ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter),
			Pilot = new PilotBattleSnapshot(
				pilotId,
				PilotCapabilityGrant.FromBackground(
					pilotId,
					EPilotBackgroundId.CyberJunkie,
					BattlePilotCapabilityPipelineFactory.Supports)),
			Team = ETeam.Player,
			Faction = EFaction.Player,
			Position = Coord.Zero,
			ExecutionAgent = new UserExecutionAgent(),
		};

		var unit = Factory.Create(spawn);
		var baselineClear = spawn.Ship.Spec.Heat.ClearPerTurn;
		var expectedClear = (int)System.Math.Round(
			baselineClear * (100 + PilotSkillPercents.CyberJunkie.HeatClearBonusPercent) / 100m,
			MidpointRounding.AwayFromZero);

		Assert.Equal(expectedClear, unit.State.HeatProfile.ClearPerTurn);
		Assert.Equal(baselineClear, spawn.Ship.Spec.Heat.ClearPerTurn);
		Assert.Equal(spawn.Ship.Spec.Heat.GainMultiplier, unit.State.HeatProfile.GainMultiplier);
	}

	[Fact]
	public void CreateBattleUnit_StacksHeatClearBonusAdditively()
	{
		var pilotId = "pilot-a";
		var grant = Assert.Single(
			PilotCapabilityGrant.FromBackground(
				pilotId,
				EPilotBackgroundId.CyberJunkie,
				capabilityId => capabilityId == EPilotCapabilityId.HeatClearBonus));
		var spawn = new BattleSpawn
		{
			Ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter),
			Pilot = new PilotBattleSnapshot(pilotId, [grant, grant]),
			Team = ETeam.Player,
			Faction = EFaction.Player,
			Position = Coord.Zero,
			ExecutionAgent = new UserExecutionAgent(),
		};

		var unit = Factory.Create(spawn);
		var baselineClear = spawn.Ship.Spec.Heat.ClearPerTurn;
		var expectedClear = (int)System.Math.Round(
			baselineClear
			* (100 + 2 * PilotSkillPercents.CyberJunkie.HeatClearBonusPercent)
			/ 100m,
			MidpointRounding.AwayFromZero);

		Assert.Equal(expectedClear, unit.State.HeatProfile.ClearPerTurn);
	}
}
