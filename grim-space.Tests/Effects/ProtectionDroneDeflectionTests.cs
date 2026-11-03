using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.Effects;

[BattleTestSuite]
public sealed class ProtectionDroneDeflectionTests
{
	[Fact]
	public void Apply_ProtectionDroneBuffStacksWithDefensiveManeuvers()
	{
		var (player, enemy, battle, hitFace) = DamageBattle();
		enemy.State.ShieldPoints[hitFace] = 0;
		BattlePassiveStatusInstaller.InstallFromCapabilities(
			enemy.State,
			ActorRuntime.From(PilotBattleSnapshot.FromSkills(
				"pilot",
				[EPilotSkillId.DefensiveManeuvers])));
		enemy.State.DefensiveManeuversMpUsed = 0;
		enemy.State.DefensiveManeuversRollIndex = 6;
		GrantProtectionDroneBuff(enemy.State, battle);
		var hullBefore = enemy.State.HullPoints;
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.LightningCannonBurst,
			new HashSet<Coord> { enemy.State.Position },
			200,
			player.State.Position);

		effect.Apply(
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(player.State.Id),
			player.State.Id);

		Assert.Equal(hullBefore - 90, enemy.State.HullPoints);
		Assert.Equal(7, enemy.State.DefensiveManeuversRollIndex);
	}

	[Fact]
	public void Apply_ProtectionDroneAloneRetainsConfiguredDamagePercent()
	{
		var (player, enemy, battle, hitFace) = DamageBattle();
		enemy.State.ShieldPoints[hitFace] = 0;
		GrantProtectionDroneBuff(enemy.State, battle);
		var hullBefore = enemy.State.HullPoints;
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.LightningCannonBurst,
			new HashSet<Coord> { enemy.State.Position },
			200,
			player.State.Position);

		effect.Apply(
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(player.State.Id),
			player.State.Id);

		Assert.Equal(hullBefore - 180, enemy.State.HullPoints);
	}

	private static void GrantProtectionDroneBuff(State target, BattleOrchestrator battle) =>
		_ = new SetProtectionDroneBuffActiveEffect(target.Id, active: true).Apply(
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(target.Id),
			target.Id);

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
