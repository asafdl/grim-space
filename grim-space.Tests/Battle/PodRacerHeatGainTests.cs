using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.PilotCapabilities;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.PilotCapabilities;

public sealed class PodRacerHeatGainTests
{
	[Fact]
	public void CreateBattleUnit_AppliesHeatGainReductionMultiplicatively()
	{
		var pilotId = "pilot-a";
		var spawn = new BattleSpawn
		{
			Ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter),
			Pilot = new PilotBattleSnapshot(
				pilotId,
				PilotCapabilityGrant.FromBackground(
					pilotId,
					EPilotBackgroundId.PodRacer,
					BattlePilotCapabilityPipelineFactory.Supports)),
			Team = ETeam.Player,
			Position = Coord.Zero,
			ExecutionAgent = new UserExecutionAgent(),
		};

		var unit = Factory.Create(spawn);
		var baselineGain = spawn.Ship.Spec.Heat.GainMultiplier;
		var retained = (100 - PilotSkillPercents.PodRacer.HeatGainReductionPercent) / 100f;

		Assert.Equal(baselineGain * retained, unit.State.HeatProfile.GainMultiplier);
		Assert.Equal(8, unit.State.HeatProfile.ScaleGain(10));
		Assert.Equal(baselineGain, spawn.Ship.Spec.Heat.GainMultiplier);
	}

	[Fact]
	public void CreateBattleUnit_StacksHeatGainReductionMultiplicatively()
	{
		var pilotId = "pilot-a";
		var grant = Assert.Single(
			PilotCapabilityGrant.FromBackground(
				pilotId,
				EPilotBackgroundId.PodRacer,
				BattlePilotCapabilityPipelineFactory.Supports));
		var spawn = new BattleSpawn
		{
			Ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter),
			Pilot = new PilotBattleSnapshot(pilotId, [grant, grant]),
			Team = ETeam.Player,
			Position = Coord.Zero,
			ExecutionAgent = new UserExecutionAgent(),
		};

		var unit = Factory.Create(spawn);
		var retained = (100 - PilotSkillPercents.PodRacer.HeatGainReductionPercent) / 100f;

		Assert.Equal(
			spawn.Ship.Spec.Heat.GainMultiplier * retained * retained,
			unit.State.HeatProfile.GainMultiplier);
		Assert.Equal(6, unit.State.HeatProfile.ScaleGain(10));
	}
}
