using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.NonUnits;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.PilotCapabilities;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;

namespace GrimSpace.Tests.StatusModel;

public sealed class ScrapDroneSwarmDamageBonusTests
{
	[Fact]
	public void StatusPass_StampsScrapDroneBonus()
	{
		var player = BattleTestFixture.Player(Coord.Zero);
		var enemy = BattleTestFixture.Enemy(new Coord(1, 0, 0));
		var world = BattleWorldFor(player, enemy);
		var runtime = ActorRuntime.From(new PilotBattleSnapshot(
			"pilot",
			PilotCapabilityGrant.FromBackground(
				"pilot",
				EPilotBackgroundId.ScrapDrone,
				BattlePilotCapabilities.Supports)));
		BattleStatusMaterializer.Materialize(world, player.State.Id, runtime);
		IAction action = new ScrapDroneSwarmAction(player.State.Id, ESpatialOrientation.Forward);
		var effect = new DamageUnitEffect(
			EImpactCause.ScrapDroneSwarmBurst,
			enemy.State.Id,
			Coord.Zero,
			100);

		var modified = BattleEffectTestKit.PrepareDamage(action, world, runtime, effect);

		Assert.Equal(PilotSkillPercents.ScrapDrone.SwarmDamageBonusPercent, modified.BonusPercent);
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
