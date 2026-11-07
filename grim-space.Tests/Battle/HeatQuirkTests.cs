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

public sealed class HeatQuirkTests
{
	[Fact]
	public void IceGears_AppliesHeatGainReduction()
	{
		var spawn = QuirkSpawn(EPilotQuirkId.IceGears);
		var unit = Factory.Create(spawn);
		var retained = (100 - PilotSkillPercents.PodRacer.HeatGainReductionPercent) / 100f;

		Assert.Equal(
			spawn.Ship.Spec.Heat.GainMultiplier * retained,
			unit.State.HeatProfile.GainMultiplier);
	}

	[Fact]
	public void FurnaceCore_AppliesHeatGainBonus()
	{
		var spawn = QuirkSpawn(EPilotQuirkId.FurnaceCore);
		var unit = Factory.Create(spawn);
		var bonus = (100 + PilotSkillPercents.SynthSinger.HeatGainBonusPercent) / 100f;

		Assert.Equal(
			spawn.Ship.Spec.Heat.GainMultiplier * bonus,
			unit.State.HeatProfile.GainMultiplier);
	}

	[Fact]
	public void RadiatorFanatic_AppliesFifteenPercentHeatClearBonus()
	{
		var spawn = QuirkSpawn(EPilotQuirkId.RadiatorFanatic);
		var unit = Factory.Create(spawn);
		var baselineClear = spawn.Ship.Spec.Heat.ClearPerTurn;
		var expectedClear = (int)System.Math.Round(
			baselineClear * (100 + PilotSkillPercents.RadiatorFanatic.HeatClearBonusPercent) / 100m,
			MidpointRounding.AwayFromZero);

		Assert.Equal(expectedClear, unit.State.HeatProfile.ClearPerTurn);
		Assert.NotEqual(
			(int)System.Math.Round(
				baselineClear * (100 + PilotSkillPercents.CyberJunkie.HeatClearBonusPercent) / 100m,
				MidpointRounding.AwayFromZero),
			unit.State.HeatProfile.ClearPerTurn);
	}

	[Fact]
	public void CloggedVents_AppliesHeatClearPenaltyMultiplicatively()
	{
		var spawn = QuirkSpawn(EPilotQuirkId.CloggedVents);
		var unit = Factory.Create(spawn);
		var expectedClear = (int)System.Math.Round(
			spawn.Ship.Spec.Heat.ClearPerTurn
			* PilotSkillPercents.CloggedVents.HeatClearTakenPercent
			/ 100m,
			MidpointRounding.AwayFromZero);

		Assert.Equal(expectedClear, unit.State.HeatProfile.ClearPerTurn);
	}

	[Fact]
	public void CloggedVents_StacksHeatClearPenaltyMultiplicatively()
	{
		var grant = Assert.Single(
			PilotCapabilityGrant.FromQuirks(
				"pilot-a",
				[EPilotQuirkId.CloggedVents],
				id => id == EPilotCapabilityId.HeatClearPenalty));
		var spawn = new BattleSpawn
		{
			Ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter),
			Pilot = new PilotBattleSnapshot("pilot-a", [grant, grant]),
			Team = ETeam.Player,
			Faction = EFaction.Player,
			Position = Coord.Zero,
			ExecutionAgent = new UserExecutionAgent(),
		};

		var unit = Factory.Create(spawn);
		var retain = PilotSkillPercents.CloggedVents.HeatClearTakenPercent;
		var once = (int)System.Math.Round(
			spawn.Ship.Spec.Heat.ClearPerTurn * retain / 100m,
			MidpointRounding.AwayFromZero);
		var twice = (int)System.Math.Round(once * retain / 100m, MidpointRounding.AwayFromZero);

		Assert.Equal(twice, unit.State.HeatProfile.ClearPerTurn);
	}

	private static BattleSpawn QuirkSpawn(EPilotQuirkId quirkId)
	{
		const string pilotId = "pilot-a";
		return new BattleSpawn
		{
			Ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter),
			Pilot = new PilotBattleSnapshot(
				pilotId,
				PilotCapabilityGrant.FromQuirks(
					pilotId,
					[quirkId],
					BattlePilotCapabilityPipelineFactory.Supports)),
			Team = ETeam.Player,
			Faction = EFaction.Player,
			Position = Coord.Zero,
			ExecutionAgent = new UserExecutionAgent(),
		};
	}
}
