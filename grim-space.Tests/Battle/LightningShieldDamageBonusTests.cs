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

public sealed class LightningShieldDamageBonusTests
{
	[Fact]
	public void StatusPass_StampsShieldBonusOnTaggedLightningAttack()
	{
		var player = BattleTestFixture.Player(Coord.Zero);
		var enemy = BattleTestFixture.Enemy(new Coord(1, 0, 0));
		var world = BattleWorldFor(player, enemy);
		var runtime = ActorRuntime.From(PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.IonizedPayload]));
		BattleStatusMaterializer.Materialize(world, player.State.Id, runtime);
		IAction action = new LightningCannonAction(player.State.Id);
		var effect = new DamageUnitEffect(
			EImpactCause.LightningCannonBurst,
			enemy.State.Id,
			Coord.Zero,
			2);

		var modified = BattleEffectTestKit.PrepareDamage(action, world, runtime, effect);

		Assert.Equal(
			PilotSkillPercents.IonizedPayload.ShieldDamageBonusPercent,
			modified.ShieldDamageBonusPercent);
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
