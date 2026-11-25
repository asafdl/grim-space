using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Effects;

[BattleTestSuite]
public sealed class ProtectionDroneResistanceTests
{
	[Fact]
	public void Apply_ProtectionDroneBuffStacksWithDefensiveManeuvers()
	{
		var (player, enemy, battle, hitFace) = DamageBattle();
		enemy.State.CurrentShieldPoints[hitFace] = 0;
		BattleStatusMaterializer.Materialize(
			battle.Engine.World,
			enemy.State.Id,
			ActorRuntime.From(PilotBattleSnapshot.FromSkills(
				"pilot",
				[EPilotSkillId.DefensiveManeuvers])));
		enemy.State.DefensiveManeuversMpUsed = 0;
		GrantProtectionDroneBuff(enemy.State, battle);
		var hullBefore = enemy.State.CurrentHullPoints;

		ApplyDamage(player, enemy, battle, 200);

		Assert.Equal(hullBefore - 162, enemy.State.CurrentHullPoints);
	}

	[Fact]
	public void Apply_ProtectionDroneAloneRetainsConfiguredDamagePercent()
	{
		var (player, enemy, battle, hitFace) = DamageBattle();
		enemy.State.CurrentShieldPoints[hitFace] = 0;
		GrantProtectionDroneBuff(enemy.State, battle);
		var hullBefore = enemy.State.CurrentHullPoints;

		ApplyDamage(player, enemy, battle, 200);

		Assert.Equal(hullBefore - 180, enemy.State.CurrentHullPoints);
	}

	private static void GrantProtectionDroneBuff(State target, BattleOrchestrator battle) =>
		_ = new SetProtectionDroneBuffActiveEffect(target.Id, active: true).Apply(
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(target.Id),
			target.Id);

	private static void ApplyDamage(Unit source, Unit target, BattleOrchestrator battle, int damage)
	{
		var action = new HighVelocityRodAction(source.State.Id, ESpatialOrientation.Forward);
		BattleEffectTestKit.ApplyEffects(
			action,
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(source.State.Id),
			[
				new DamageUnitEffect(
					EImpactCause.HighVelocityRod,
					target.State.Id,
					source.State.Position,
					damage),
			]);
	}

	private static (Unit Player, Unit Enemy, BattleOrchestrator Battle, ESpatialOrientation HitFace) DamageBattle()
	{
		var player = Factory.Create(
			ShipInstance.FromCatalog("attacker", EType.Fighter),
			ETeam.Player,
			new Coord(5, 5, 5),
			new GrimSpace.Battle.Player.UserExecutionAgent());
		var enemy = BattleTestFixture.Enemy(new Coord(6, 5, 5));
		var hitFace = GrimSpace.Battle.Spatial.BodyFrame.From(enemy.State).HitFaceFrom(player.State.Position);
		var battle = BattleTestFixture.BeginSimulation(player, enemy);
		return (player, enemy, battle, hitFace);
	}
}
