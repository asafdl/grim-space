using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Effects;

[BattleTestSuite]
public sealed class ApplyAreaDamageEffectTests
{
	[Theory]
	[InlineData(0, 245)]
	[InlineData(20, 225)]
	[InlineData(69, 176)]
	[InlineData(70, 0)]
	[InlineData(100, 0)]
	public void HullBreachBonusMultipliesEntireShotOnlyWhenBaseHitExceedsShields(
		int shields, int expectedHullDamage)
	{
		var (player, enemy, battle, face) = DamageBattle();
		enemy.State.CurrentHullPoints = 1000;
		enemy.State.CurrentShieldPoints[face] = shields;
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.HighSpeedRod,
			new HashSet<Coord> { enemy.State.Position },
			70,
			player.State.Position,
			hullBreachDamagePercent: 350);
		var runtime = battle.Engine.ActorRuntimes.For(player.State.Id);

		effect.Apply(battle.Engine.World, runtime, player.State.Id);

		Assert.Equal(1000 - expectedHullDamage, enemy.State.CurrentHullPoints);
		Assert.Equal(System.Math.Max(0, shields - 70), enemy.State.CurrentShieldPoints[face]);
		effect.Undo(battle.Engine.World, runtime, player.State.Id);
		Assert.Equal(1000, enemy.State.CurrentHullPoints);
		Assert.Equal(shields, enemy.State.CurrentShieldPoints[face]);
	}

	[Fact]
	public void DamageRewritesPreserveHullBreachMultiplier()
	{
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.HighSpeedRod, new HashSet<Coord>(), 70, Coord.Zero,
			hullBreachDamagePercent: 350);
		Assert.All(
			new[]
			{
				effect.WithAdditionalDamage(10),
				effect.WithAdditionalShieldDamage(10),
				effect.WithShieldDamageBonusPercent(20),
				effect.WithAdjacentHitDamagePercent(150),
				effect.WithDamageBonusPercent(20),
				effect.WithAddedDamageBonusPercent(20),
			},
			rewritten => Assert.Equal(350, rewritten.HullBreachDamagePercent));
	}

	[Fact]
	public void WithAdditionalShieldDamage_AddsOnlyToShieldBonus()
	{
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.LightningCannonBurst,
			new HashSet<Coord>(),
			3,
			Coord.Zero,
			1);
		var adjusted = effect.WithAdditionalShieldDamage(2);

		Assert.Equal(3, adjusted.Damage);
		Assert.Equal(3, adjusted.ShieldDamageBonus);
	}

	[Fact]
	public void WithAdjacentHitDamagePercent_SetsMultiplierOnly()
	{
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.LightningCannonBurst,
			new HashSet<Coord>(),
			3,
			Coord.Zero,
			1);

		var adjusted = effect.WithAdjacentHitDamagePercent(150);

		Assert.Equal(3, adjusted.Damage);
		Assert.Equal(1, adjusted.ShieldDamageBonus);
		Assert.Equal(150, adjusted.AdjacentHitDamagePercent);
	}

	[Fact]
	public void Apply_UsesShieldDamageBonusOnlyAgainstShields()
	{
		var player = Factory.Create(
			ShipInstance.FromCatalog("attacker", EType.Fighter),
			ETeam.Player,
			new Coord(5, 5, 5),
			new GrimSpace.Battle.Player.UserExecutionAgent());
		var enemy = BattleTestFixture.Enemy(new Coord(6, 5, 5));
		var hitFace = GrimSpace.Battle.Spatial.BodyFrame.From(enemy.State).HitFaceFrom(player.State.Position);
		enemy.State.CurrentShieldPoints[hitFace] = 2;
		var hullBefore = enemy.State.CurrentHullPoints;
		var battle = BattleTestFixture.BeginSimulation(player, enemy);
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.LightningCannonBurst,
			new HashSet<Coord> { enemy.State.Position },
			2,
			player.State.Position,
			1);

		var runtime = battle.Engine.ActorRuntimes.For(player.State.Id);
		effect.Apply(battle.Engine.World, runtime, player.State.Id);

		Assert.Equal(hullBefore, enemy.State.CurrentHullPoints);
		Assert.Equal(0, enemy.State.CurrentShieldPoints[hitFace]);

		effect.Undo(battle.Engine.World, runtime, player.State.Id);
		Assert.Equal(2, enemy.State.CurrentShieldPoints[hitFace]);
	}

	[Fact]
	public void Apply_DefensiveManeuversReducesDamageAndUndoRestoresHull()
	{
		var (player, enemy, battle, hitFace) = DamageBattle();
		enemy.State.CurrentShieldPoints[hitFace] = 0;
		InstallDefensiveManeuvers(battle.Engine.World, enemy.State.Id);
		enemy.State.DefensiveManeuversMpUsed = 0;
		var hullBefore = enemy.State.CurrentHullPoints;
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.LightningCannonBurst,
			new HashSet<Coord> { enemy.State.Position },
			200,
			player.State.Position);
		var runtime = battle.Engine.ActorRuntimes.For(player.State.Id);

		effect.Apply(battle.Engine.World, runtime, player.State.Id);

		Assert.Equal(hullBefore - 180, enemy.State.CurrentHullPoints);

		effect.Undo(battle.Engine.World, runtime, player.State.Id);
		Assert.Equal(hullBefore, enemy.State.CurrentHullPoints);
	}

	[Fact]
	public void Apply_DefensiveManeuversInactiveAfterSpendingMp()
	{
		var (player, enemy, battle, hitFace) = DamageBattle();
		enemy.State.CurrentShieldPoints[hitFace] = 0;
		InstallDefensiveManeuvers(battle.Engine.World, enemy.State.Id);
		enemy.State.DefensiveManeuversMpUsed = 1;
		var hullBefore = enemy.State.CurrentHullPoints;
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.LightningCannonBurst,
			new HashSet<Coord> { enemy.State.Position },
			200,
			player.State.Position);

		effect.Apply(
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(player.State.Id),
			player.State.Id);

		Assert.Equal(hullBefore - 200, enemy.State.CurrentHullPoints);
	}

	[Fact]
	public void Apply_RangeHitDamageBonusAppliesPerTargetAndUndoRestoresBoth()
	{
		var player = Factory.Create(
			ShipInstance.FromCatalog("attacker", EType.Fighter),
			ETeam.Player,
			new Coord(5, 5, 5),
			new GrimSpace.Battle.Player.UserExecutionAgent());
		var adjacent = BattleTestFixture.Enemy(new Coord(6, 5, 5));
		var distant = Factory.Create(
			ShipInstance.FromCatalog("distant", EType.Fighter),
			ETeam.Enemy,
			new Coord(7, 5, 5),
			BattleAgentFactory.Create(EBattleAgentKind.Ai));
		var adjacentFace = GrimSpace.Battle.Spatial.BodyFrame.From(adjacent.State)
			.HitFaceFrom(player.State.Position);
		var distantFace = GrimSpace.Battle.Spatial.BodyFrame.From(distant.State)
			.HitFaceFrom(player.State.Position);
		adjacent.State.CurrentShieldPoints[adjacentFace] = 6;
		distant.State.CurrentShieldPoints[distantFace] = 6;
		var adjacentHullBefore = adjacent.State.CurrentHullPoints;
		var distantHullBefore = distant.State.CurrentHullPoints;
		var battle = BattleTestFixture.BeginSimulation(player, [adjacent, distant]);
		var effect = new ApplyAreaDamageEffect(
				EImpactCause.LightningCannonBurst,
				new HashSet<Coord> { adjacent.State.Position, distant.State.Position },
				2,
				player.State.Position)
			.WithAdjacentHitDamagePercent(200, rangeCells: 2);
		var runtime = battle.Engine.ActorRuntimes.For(player.State.Id);

		effect.Apply(battle.Engine.World, runtime, player.State.Id);

		Assert.Equal(4, adjacent.State.CurrentShieldPoints[adjacentFace]);
		Assert.Equal(2, distant.State.CurrentShieldPoints[distantFace]);
		Assert.Equal(adjacentHullBefore, adjacent.State.CurrentHullPoints);
		Assert.Equal(distantHullBefore, distant.State.CurrentHullPoints);

		effect.Undo(battle.Engine.World, runtime, player.State.Id);
		Assert.Equal(6, adjacent.State.CurrentShieldPoints[adjacentFace]);
		Assert.Equal(6, distant.State.CurrentShieldPoints[distantFace]);
		Assert.Equal(adjacentHullBefore, adjacent.State.CurrentHullPoints);
		Assert.Equal(distantHullBefore, distant.State.CurrentHullPoints);
	}

	private static (
		Unit Player,
		Unit Enemy,
		GrimSpace.Battle.BattleOrchestrator Battle,
		ESpatialOrientation HitFace) DamageBattle()
	{
		var player = Factory.Create(
			ShipInstance.FromCatalog("attacker", EType.Fighter),
			ETeam.Player,
			new Coord(5, 5, 5),
			new GrimSpace.Battle.Player.UserExecutionAgent());
		var enemy = BattleTestFixture.Enemy(new Coord(6, 5, 5));
		var battle = BattleTestFixture.BeginSimulation(player, enemy);
		var hitFace = GrimSpace.Battle.Spatial.BodyFrame.From(enemy.State)
			.HitFaceFrom(player.State.Position);
		return (player, enemy, battle, hitFace);
	}

	private static void InstallDefensiveManeuvers(BattleWorld world, string unitId) =>
		BattlePassiveStatusInstaller.InstallFromCapabilities(
			world,
			unitId,
			ActorRuntime.From(PilotBattleSnapshot.FromSkills(
				"pilot",
				[EPilotSkillId.DefensiveManeuvers])));
}
