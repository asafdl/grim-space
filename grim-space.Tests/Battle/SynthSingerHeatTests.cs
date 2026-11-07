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

public sealed class SynthSingerHeatTests
{
	[Fact]
	public void CreateBattleUnit_AppliesHeatGainAndCapacityBonusesAdditively()
	{
		var pilotId = "pilot-a";
		var spawn = new BattleSpawn
		{
			Ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter),
			Pilot = new PilotBattleSnapshot(
				pilotId,
				PilotCapabilityGrant.FromBackground(
					pilotId,
					EPilotBackgroundId.SynthSinger,
					BattlePilotCapabilityPipelineFactory.Supports)),
			Team = ETeam.Player,
			Faction = EFaction.Player,
			Position = Coord.Zero,
			ExecutionAgent = new UserExecutionAgent(),
		};

		var unit = Factory.Create(spawn);
		var baseline = spawn.Ship.Spec.Heat;
		var expectedGain =
			baseline.GainMultiplier
			* (100 + PilotSkillPercents.SynthSinger.HeatGainBonusPercent)
			/ 100f;
		var expectedCapacity = (int)System.Math.Round(
			baseline.EffectiveCapacity
			* (100 + PilotSkillPercents.SynthSinger.HeatCapacityBonusPercent)
			/ 100m,
			MidpointRounding.AwayFromZero);

		Assert.Equal(expectedGain, unit.State.HeatProfile.GainMultiplier);
		Assert.Equal(12, unit.State.HeatProfile.ScaleGain(10));
		Assert.Equal(expectedCapacity, unit.State.HeatProfile.EffectiveCapacity);
		Assert.Equal(baseline.GainMultiplier, spawn.Ship.Spec.Heat.GainMultiplier);
		Assert.Equal(baseline.EffectiveCapacity, spawn.Ship.Spec.Heat.EffectiveCapacity);
		Assert.Equal(baseline.ClearPerTurn, unit.State.HeatProfile.ClearPerTurn);
	}

	[Fact]
	public void CreateBattleUnit_StacksHeatGainAndCapacityBonusesAdditively()
	{
		var pilotId = "pilot-a";
		var grants = PilotCapabilityGrant.FromBackground(
			pilotId,
			EPilotBackgroundId.SynthSinger,
			BattlePilotCapabilityPipelineFactory.Supports);
		var spawn = new BattleSpawn
		{
			Ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter),
			Pilot = new PilotBattleSnapshot(pilotId, [..grants, ..grants]),
			Team = ETeam.Player,
			Faction = EFaction.Player,
			Position = Coord.Zero,
			ExecutionAgent = new UserExecutionAgent(),
		};

		var unit = Factory.Create(spawn);
		var baseline = spawn.Ship.Spec.Heat;
		var expectedGain =
			baseline.GainMultiplier
			* (100 + 2 * PilotSkillPercents.SynthSinger.HeatGainBonusPercent)
			/ 100f;
		var expectedCapacity = (int)System.Math.Round(
			baseline.EffectiveCapacity
			* (100 + 2 * PilotSkillPercents.SynthSinger.HeatCapacityBonusPercent)
			/ 100m,
			MidpointRounding.AwayFromZero);

		Assert.Equal(expectedGain, unit.State.HeatProfile.GainMultiplier);
		Assert.Equal(14, unit.State.HeatProfile.ScaleGain(10));
		Assert.Equal(expectedCapacity, unit.State.HeatProfile.EffectiveCapacity);
	}
}
