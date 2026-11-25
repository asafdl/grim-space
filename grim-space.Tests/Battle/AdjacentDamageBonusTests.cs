using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.NonUnits;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;

namespace GrimSpace.Tests.StatusModel;

public sealed class AdjacentDamageBonusTests
{
	[Fact]
	public void StatusPass_ScalesDamageAtBonusRange()
	{
		var player = BattleTestFixture.Player(Coord.Zero);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 2, 0));
		var world = BattleWorldFor(player, enemy);
		var runtime = ActorRuntime.From(PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.DodgeThis]));
		BattleStatusMaterializer.Materialize(world, player.State.Id, runtime);
		IAction action = new HighVelocityRodAction(player.State.Id, ESpatialOrientation.Forward);
		var effect = new DamageUnitEffect(
			EImpactCause.HighVelocityRod,
			enemy.State.Id,
			Coord.Zero,
			100);

		var modified = BattleEffectTestKit.PrepareDamage(action, world, runtime, effect);

		Assert.Equal(
			[PilotSkillPercents.DodgeThis.BonusHitDamagePercent],
			modified.DamagePercents);
	}

	[Fact]
	public void StatusPass_DoesNotScaleWhenOutOfBonusRange()
	{
		var player = BattleTestFixture.Player(Coord.Zero);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 3, 0));
		var world = BattleWorldFor(player, enemy);
		var runtime = ActorRuntime.From(PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.DodgeThis]));
		BattleStatusMaterializer.Materialize(world, player.State.Id, runtime);
		IAction action = new HighVelocityRodAction(player.State.Id, ESpatialOrientation.Forward);
		var effect = new DamageUnitEffect(
			EImpactCause.HighVelocityRod,
			enemy.State.Id,
			Coord.Zero,
			100);

		var modified = BattleEffectTestKit.PrepareDamage(action, world, runtime, effect);

		Assert.Empty(modified.DamagePercents);
	}

	private static BattleWorld BattleWorldFor(Unit player, Unit enemy) =>
		BattleWorld.FromLive(
			[player, enemy],
			new Dictionary<string, NonUnit>(),
			BattleTestFixture.Grid(),
			new HashSet<Coord>(),
			"status-test",
			EObjective.EliminateOpponents,
			new HashSet<string> { player.State.Id, enemy.State.Id },
			new Timeline());
}
