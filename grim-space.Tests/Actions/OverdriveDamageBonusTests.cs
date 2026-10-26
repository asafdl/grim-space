using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.PilotCapabilities;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Heat;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.Actions;

[BattleTestSuite]
public sealed class OverdriveDamageBonusTests
{
	private const string PlayerId = "player";

	[Fact]
	public void ThresholdCrossingAttack_GetsDamageBonus()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.Heat = state.HeatProfile.EffectiveCapacity - HeatChangeEffect.PerAbilityUse;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);

		var modified = runtime.ActionEffects.Apply(
			AttackAction(),
			battle.Engine.World,
			runtime,
			[SampleAreaDamage()]);

		Assert.True(OverdriveDamageBonusModifier.IsEligible(battle.Engine.World, PlayerId));
		Assert.Equal(
			OverdriveDamageBonusModifier.BonusPercent,
			Assert.IsType<ApplyAreaDamageEffect>(modified[0]).DamageBonusPercent);
	}

	[Fact]
	public void PriorAttackBelowThreshold_Unbuffed()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.Heat = 0;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);

		var modified = runtime.ActionEffects.Apply(
			AttackAction(),
			battle.Engine.World,
			runtime,
			[SampleAreaDamage()]);

		Assert.False(OverdriveDamageBonusModifier.IsEligible(battle.Engine.World, PlayerId));
		Assert.Equal(0, Assert.IsType<ApplyAreaDamageEffect>(modified[0]).DamageBonusPercent);
	}

	[Fact]
	public void OverdriveFlag_BuffsAttack()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.Heat = 50;
		state.Overdrive = true;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);

		var modified = runtime.ActionEffects.Apply(
			AttackAction(),
			battle.Engine.World,
			runtime,
			[SampleAreaDamage()]);

		Assert.Equal(
			OverdriveDamageBonusModifier.BonusPercent,
			Assert.IsType<ApplyAreaDamageEffect>(modified[0]).DamageBonusPercent);
	}

	[Fact]
	public void HeatSpecNone_NeverGetsBonusEvenIfOverdrive()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.HeatProfile = HeatSpec.None;
		state.Overdrive = true;
		state.Heat = 100;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);

		var modified = runtime.ActionEffects.Apply(
			AttackAction(),
			battle.Engine.World,
			runtime,
			[SampleAreaDamage()]);

		Assert.False(OverdriveDamageBonusModifier.IsEligible(battle.Engine.World, PlayerId));
		Assert.Equal(0, Assert.IsType<ApplyAreaDamageEffect>(modified[0]).DamageBonusPercent);
	}

	[Fact]
	public void EmptyPilotRuntime_StillHasOverdriveModifierPipeline()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.Overdrive = true;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);

		Assert.Empty(runtime.PilotCapabilities);

		var modified = runtime.ActionEffects.Apply(
			AttackAction(),
			battle.Engine.World,
			runtime,
			[SampleAreaDamage()]);

		Assert.Equal(
			OverdriveDamageBonusModifier.BonusPercent,
			Assert.IsType<ApplyAreaDamageEffect>(modified[0]).DamageBonusPercent);
	}

	[Fact]
	public void DetonateAction_HasNoAttackTagSoPipelineSkipsBonus()
	{
		IAction action = new DetonateAction("void-bomb");
		Assert.DoesNotContain(BattleActionTags.Attack, action.Tags);
	}

	private static ScrapDroneSwarmAction AttackAction()
	{
		var mountedOn = ESpatialOrientation.Port;
		return new ScrapDroneSwarmAction(PlayerId, mountedOn);
	}

	private static ApplyAreaDamageEffect SampleAreaDamage() =>
		new(EImpactCause.ScrapDroneSwarmBurst, new HashSet<Coord>(), 100, Coord.Zero);
}
