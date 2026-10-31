using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Units;
using GrimSpace.Math;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Effects;

[BattleTestSuite]
public sealed class ProtectionDroneDeflectionTests
{
	[Fact]
	public void Apply_ProtectionDroneBuffDeflectsIndependentlyOfDefensiveManeuvers()
	{
		var (player, enemy, battle, hitFace) = DamageBattle();
		enemy.State.ShieldPoints[hitFace] = 0;
		enemy.State.DefensiveManeuversActive = true;
		enemy.State.DefensiveManeuversMpUsed = 0;
		enemy.State.DefensiveManeuversRollIndex = 6;
		enemy.State.ProtectionDroneBuffActive = true;
		var droneIndex = SucceedingProtectionDroneIndex(
			enemy.State.Id,
			EImpactCause.LightningCannonBurst);
		enemy.State.ProtectionDroneRollIndex = droneIndex;
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

		// Defensive Maneuvers (index 6) and Protection Drone both succeed → 25% retained.
		Assert.Equal(hullBefore - 50, enemy.State.HullPoints);
		Assert.Equal(7, enemy.State.DefensiveManeuversRollIndex);
		Assert.Equal(droneIndex + 1, enemy.State.ProtectionDroneRollIndex);
	}

	[Fact]
	public void Apply_ProtectionDroneAloneRetainsConfiguredDamagePercent()
	{
		var (player, enemy, battle, hitFace) = DamageBattle();
		enemy.State.ShieldPoints[hitFace] = 0;
		enemy.State.ProtectionDroneBuffActive = true;
		enemy.State.ProtectionDroneRollIndex = SucceedingProtectionDroneIndex(
			enemy.State.Id,
			EImpactCause.LightningCannonBurst);
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

		Assert.Equal(hullBefore - 100, enemy.State.HullPoints);
	}

	private static int SucceedingProtectionDroneIndex(string unitId, EImpactCause cause)
	{
		var chance = PilotSkillPercents.ProtectionDrone.DeflectChancePercent / 100.0;
		for (var index = 0; index < 256; index++)
		{
			var random = new StableRandom(
				new StableSeedMixer()
					.Add(unitId)
					.Add(index)
					.Add((int)cause)
					.Add(1)
					.Value);
			if (random.NextDouble() < chance)
				return index;
		}

		throw new InvalidOperationException("No succeeding Protection Drone roll index found.");
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
