using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Effects;

[BattleTestSuite]
public sealed class ApplyAreaDamageEffectTests
{
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
		enemy.State.ShieldPoints[hitFace] = 2;
		var hullBefore = enemy.State.HullPoints;
		var battle = BattleTestFixture.BeginSimulation(player, enemy);
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.LightningCannonBurst,
			new HashSet<Coord> { enemy.State.Position },
			2,
			player.State.Position,
			1);

		var runtime = battle.Engine.ActorRuntimes.For(player.State.Id);
		effect.Apply(battle.Engine.World, runtime, player.State.Id);

		Assert.Equal(hullBefore, enemy.State.HullPoints);
		Assert.Equal(0, enemy.State.ShieldPoints[hitFace]);

		effect.Undo(battle.Engine.World, runtime, player.State.Id);
		Assert.Equal(2, enemy.State.ShieldPoints[hitFace]);
	}

	[Theory]
	[InlineData(0, 2)]
	[InlineData(6, 1)]
	public void Apply_DefensiveManeuversRollHalvesDamage(
		int rollIndex,
		int expectedHullDamage)
	{
		var (player, enemy, battle, hitFace) = DamageBattle();
		enemy.State.ShieldPoints[hitFace] = 0;
		enemy.State.DefensiveManeuversMpSpentLastTurn = 0;
		enemy.State.DefensiveManeuversRollIndex = rollIndex;
		var hullBefore = enemy.State.HullPoints;
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.LightningCannonBurst,
			new HashSet<Coord> { enemy.State.Position },
			2,
			player.State.Position);
		var runtime = battle.Engine.ActorRuntimes.For(player.State.Id);

		effect.Apply(battle.Engine.World, runtime, player.State.Id);

		Assert.Equal(hullBefore - expectedHullDamage, enemy.State.HullPoints);
		Assert.Equal(rollIndex + 1, enemy.State.DefensiveManeuversRollIndex);

		effect.Undo(battle.Engine.World, runtime, player.State.Id);
		Assert.Equal(hullBefore, enemy.State.HullPoints);
		Assert.Equal(rollIndex, enemy.State.DefensiveManeuversRollIndex);
	}

	[Fact]
	public void Apply_DoesNotRollDeflectionWithoutQualifyingTurn()
	{
		var (player, enemy, battle, hitFace) = DamageBattle();
		enemy.State.ShieldPoints[hitFace] = 0;
		enemy.State.DefensiveManeuversRollIndex = 6;
		var hullBefore = enemy.State.HullPoints;
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.LightningCannonBurst,
			new HashSet<Coord> { enemy.State.Position },
			2,
			player.State.Position);

		effect.Apply(
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(player.State.Id),
			player.State.Id);

		Assert.Equal(hullBefore - 2, enemy.State.HullPoints);
		Assert.Equal(6, enemy.State.DefensiveManeuversRollIndex);
	}

	[Fact]
	public void Apply_DeflectionRetainsConfiguredDamagePercent()
	{
		var (player, enemy, battle, hitFace) = DamageBattle();
		enemy.State.ShieldPoints[hitFace] = 0;
		enemy.State.DefensiveManeuversMpSpentLastTurn = 0;
		enemy.State.DefensiveManeuversRollIndex = 6;
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
		adjacent.State.ShieldPoints[adjacentFace] = 6;
		distant.State.ShieldPoints[distantFace] = 6;
		var adjacentHullBefore = adjacent.State.HullPoints;
		var distantHullBefore = distant.State.HullPoints;
		var battle = BattleTestFixture.BeginSimulation(player, [adjacent, distant]);
		var effect = new ApplyAreaDamageEffect(
				EImpactCause.LightningCannonBurst,
				new HashSet<Coord> { adjacent.State.Position, distant.State.Position },
				2,
				player.State.Position)
			.WithAdjacentHitDamagePercent(200, rangeCells: 2);
		var runtime = battle.Engine.ActorRuntimes.For(player.State.Id);

		effect.Apply(battle.Engine.World, runtime, player.State.Id);

		Assert.Equal(4, adjacent.State.ShieldPoints[adjacentFace]);
		Assert.Equal(2, distant.State.ShieldPoints[distantFace]);
		Assert.Equal(adjacentHullBefore, adjacent.State.HullPoints);
		Assert.Equal(distantHullBefore, distant.State.HullPoints);

		effect.Undo(battle.Engine.World, runtime, player.State.Id);
		Assert.Equal(6, adjacent.State.ShieldPoints[adjacentFace]);
		Assert.Equal(6, distant.State.ShieldPoints[distantFace]);
		Assert.Equal(adjacentHullBefore, adjacent.State.HullPoints);
		Assert.Equal(distantHullBefore, distant.State.HullPoints);
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
}
