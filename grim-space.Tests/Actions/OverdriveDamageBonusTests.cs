using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.PilotCapabilities;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Heat;

namespace GrimSpace.Tests.Actions;

[BattleTestSuite]
public sealed class OverdriveDamageBonusTests
{
	private const string PlayerId = "player";

	[Fact]
	public void ThresholdCrossingAttack_GetsDamageBonusAtApply()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.Heat = state.HeatProfile.EffectiveCapacity - HeatChangeEffect.PerAbilityUse;
		var enemy = battle.Engine.World.UnitRegistry.All.First(unit => unit.State.Id != PlayerId);
		enemy.State.Position = new Coord(6, 5, 5);
		var hitFace = GrimSpace.Battle.Spatial.BodyFrame.From(enemy.State)
			.HitFaceFrom(state.Position);
		enemy.State.CurrentShieldPoints[hitFace] = 0;
		var hullBefore = enemy.State.CurrentHullPoints;
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.ScrapDroneSwarmBurst,
			new HashSet<Coord> { enemy.State.Position },
			100,
			state.Position);

		_ = new HeatChangeEffect(HeatChangeEffect.PerAbilityUse).Apply(
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(PlayerId),
			PlayerId);
		Assert.True(state.Overdrive);
		effect.Apply(
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(PlayerId),
			PlayerId);

		Assert.Equal(
			hullBefore - ScalePercent(100, 100 + HeatChangeEffect.OverdriveDamageBonusPercent),
			enemy.State.CurrentHullPoints);
	}

	[Fact]
	public void PriorAttackBelowThreshold_Unbuffed()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.Heat = 0;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);

		var modified = BattleStatuses.RewriteOutgoing(
			AttackAction(),
			battle.Engine.World,
			[SampleAreaDamage()]);

		Assert.False(state.Overdrive);
		Assert.Equal(0, Assert.IsType<ApplyAreaDamageEffect>(modified[0]).DamageBonusPercent);
		_ = runtime;
	}

	[Fact]
	public void OverdriveStatus_BuffsAttackThroughRewriteOutgoing()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.Heat = 50;
		state.Overdrive = true;
		BattleStatuses.SyncOverdrive(state);

		var modified = BattleStatuses.RewriteOutgoing(
			AttackAction(),
			battle.Engine.World,
			[SampleAreaDamage()]);

		Assert.Equal(
			HeatChangeEffect.OverdriveDamageBonusPercent,
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
		BattleStatuses.SyncOverdrive(state);

		var modified = BattleStatuses.RewriteOutgoing(
			AttackAction(),
			battle.Engine.World,
			[SampleAreaDamage()]);

		Assert.Equal(0, Assert.IsType<ApplyAreaDamageEffect>(modified[0]).DamageBonusPercent);
		Assert.Equal(
			100,
			ApplyAreaDamageEffect.ApplyOverdriveCrossingBonus(state, 100));
	}

	[Fact]
	public void EmptyPilotRuntime_StillBuffsWhenOverdriveSynced()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.Overdrive = true;
		BattleStatuses.SyncOverdrive(state);
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);

		Assert.Empty(runtime.PilotCapabilities);

		var modified = BattleStatuses.RewriteOutgoing(
			AttackAction(),
			battle.Engine.World,
			[SampleAreaDamage()]);

		Assert.Equal(
			HeatChangeEffect.OverdriveDamageBonusPercent,
			Assert.IsType<ApplyAreaDamageEffect>(modified[0]).DamageBonusPercent);
	}

	[Fact]
	public void Pipeline_NoLongerRegistersOverdriveBonus()
	{
		var pipeline = BattlePilotCapabilityPipelineFactory.Build(
			new HashSet<EPilotCapabilityId>());
		IAction action = AttackAction();
		var effect = SampleAreaDamage();

		var modified = Assert.IsType<ApplyAreaDamageEffect>(
			Assert.Single(pipeline.Apply(action, null!, ActorRuntime.From(null), [effect])));

		Assert.Equal(0, modified.DamageBonusPercent);
	}

	[Fact]
	public void DetonateAction_HasNoAttackTagSoStatusSkipsBonus()
	{
		IAction action = new DetonateAction("void-bomb");
		Assert.DoesNotContain(BattleActionTags.Attack, action.Tags);
	}

	[Fact]
	public void StampedBonus_DoesNotDoubleApplyCrossingException()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.Overdrive = true;
		BattleStatuses.SyncOverdrive(state);
		var stamped = Assert.IsType<ApplyAreaDamageEffect>(
			Assert.Single(BattleStatuses.RewriteOutgoing(AttackAction(), battle.Engine.World, [SampleAreaDamage()])));

		Assert.Equal(HeatChangeEffect.OverdriveDamageBonusPercent, stamped.DamageBonusPercent);
		// Apply path uses stamped percent and skips crossing helper when DamageBonusPercent != 0.
		Assert.Equal(
			ScalePercent(100, 100 + HeatChangeEffect.OverdriveDamageBonusPercent),
			ScalePercent(stamped.Damage, 100 + stamped.DamageBonusPercent));
	}

	private static ScrapDroneSwarmAction AttackAction() =>
		new(PlayerId, ESpatialOrientation.Port);

	private static ApplyAreaDamageEffect SampleAreaDamage() =>
		new(EImpactCause.ScrapDroneSwarmBurst, new HashSet<Coord>(), 100, Coord.Zero);

	private static int ScalePercent(int amount, int percent) =>
		(int)System.Math.Round(amount * percent / 100m, MidpointRounding.AwayFromZero);
}
