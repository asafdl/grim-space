using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Actions;

[BattleTestSuite]
public sealed class OverdriveDamageBonusTests
{
	private const string PlayerId = "player";

	[Fact]
	public void ThresholdCrossingAttack_GetsDamageBonusAtApply()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var world = battle.Engine.World;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);
		var state = world.StateOf(PlayerId);
		state.Heat = state.HeatProfile.EffectiveCapacity - HeatChangeEffect.PerAbilityUse;
		var enemy = world.UnitRegistry.All.First(unit => unit.State.Id != PlayerId);
		enemy.State.Position = new Coord(6, 5, 5);
		var hitFace = GrimSpace.Battle.Spatial.BodyFrame.From(enemy.State).HitFaceFrom(state.Position);
		enemy.State.CurrentShieldPoints[hitFace] = 0;
		var hullBefore = enemy.State.CurrentHullPoints;
		var action = new HighVelocityRodAction(PlayerId, ESpatialOrientation.Forward);
		var effects = new IEffect<GrimSpace.Battle.World.BattleWorld, ActorRuntime>[]
		{
			new HeatChangeEffect(HeatChangeEffect.PerAbilityUse),
			new DamageUnitEffect(EImpactCause.HighVelocityRod, enemy.State.Id, state.Position, 100),
		};

		BattleEffectTestKit.ApplyEffects(action, world, runtime, effects);

		Assert.True(state.Overdrive);
		Assert.Equal(
			hullBefore - ScalePercent(100, 100 + HeatChangeEffect.OverdriveDamageBonusPercent),
			enemy.State.CurrentHullPoints);
	}

	[Fact]
	public void OverdriveFlag_BuffsAttackThroughRules()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var world = battle.Engine.World;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);
		var state = world.StateOf(PlayerId);
		state.Overdrive = true;
		var enemy = world.UnitRegistry.All.First(unit => unit.State.Id != PlayerId);
		var action = new HighVelocityRodAction(PlayerId, ESpatialOrientation.Forward);
		var prepared = BattleEffectTestKit.PrepareDamage(
			action,
			world,
			runtime,
			new DamageUnitEffect(EImpactCause.HighVelocityRod, enemy.State.Id, state.Position, 100));

		Assert.Equal(HeatChangeEffect.OverdriveDamageBonusPercent, prepared.BonusPercent);
	}

	[Fact]
	public void NonAttackAction_DoesNotReceiveOverdriveBonus()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var world = battle.Engine.World;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);
		world.StateOf(PlayerId).Overdrive = true;
		var enemy = world.UnitRegistry.All.First(unit => unit.State.Id != PlayerId);
		var prepared = BattleEffectTestKit.PrepareDamage(
			new MoveStepAction(PlayerId),
			world,
			runtime,
			new DamageUnitEffect(EImpactCause.HighVelocityRod, enemy.State.Id, Coord.Zero, 100));

		Assert.Equal(0, prepared.BonusPercent);
	}

	private static int ScalePercent(int amount, int percent) =>
		(int)System.Math.Round(amount * percent / 100m, MidpointRounding.AwayFromZero);
}
