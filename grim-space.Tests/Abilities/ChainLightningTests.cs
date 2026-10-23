using GrimSpace.Battle;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.Abilities;

[BattleTestSuite]
public sealed class ChainLightningTests
{
	[Fact]
	public void DamageAtHop_AppliesMultiplicativeFalloff()
	{
		Assert.Equal(200, ChainLightning.DamageAtHop(200, 0, 25));
		Assert.Equal(150, ChainLightning.DamageAtHop(200, 1, 25));
		Assert.Equal(113, ChainLightning.DamageAtHop(200, 2, 25));
	}

	[Fact]
	public void Resolve_ChainsToSecondOpponentWithinRange()
	{
		var origin = new Coord(5, 5, 5);
		var first = origin + Coord.Forward * 3;
		var second = first + Coord.Up * 2;
		var player = BattleTestFixture.Player(origin);
		var battle = BattleTestFixture.BeginSimulation(
			player,
			[
				EnemyAt(first, "enemy-a"),
				EnemyAt(second, "enemy-b"),
			]);
		var spec = LightningCannonSpec.Baseline;

		var result = ChainLightning.Resolve(
			battle.Engine.World,
			player.State.Id,
			spec,
			ESpatialOrientation.Forward);

		Assert.Equal(2, result.Hops.Count);
		Assert.Equal("enemy-a", result.Hops[0].TargetId);
		Assert.Equal("enemy-b", result.Hops[1].TargetId);
	}

	[Fact]
	public void Resolve_SkipsAlliesOnBeamAndHitsOpponentBehind()
	{
		var origin = new Coord(5, 5, 5);
		var allyPos = origin + Coord.Forward * 2;
		var enemyPos = origin + Coord.Forward * 4;
		var player = BattleTestFixture.Player(origin);
		var ally = Factory.Create(
			ShipInstance.FromCatalog("ally", EType.Fighter),
			ETeam.Player,
			allyPos,
			new UserExecutionAgent());
		var battle = BattleTestFixture.BeginSimulation(
			player,
			[BattleTestFixture.Enemy(enemyPos)]);
		UnitRegistry.For(battle.Engine.World).Add(ally);

		var result = ChainLightning.Resolve(
			battle.Engine.World,
			player.State.Id,
			LightningCannonSpec.Baseline,
			ESpatialOrientation.Forward);

		Assert.Single(result.Hops);
		Assert.Equal(BattleTestFixture.FirstEnemyId(battle), result.Hops[0].TargetId);
	}

	[Fact]
	public void Resolve_DoesNotRevisitTargets()
	{
		var origin = new Coord(5, 5, 5);
		var first = origin + Coord.Forward * 2;
		var second = first + Coord.Up;
		var player = BattleTestFixture.Player(origin);
		var battle = BattleTestFixture.BeginSimulation(
			player,
			[
				EnemyAt(first, "enemy-a"),
				EnemyAt(second, "enemy-b"),
			]);
		var spec = LightningCannonSpec.Baseline with { MaxChains = 3 };

		var result = ChainLightning.Resolve(
			battle.Engine.World,
			player.State.Id,
			spec,
			ESpatialOrientation.Forward);

		Assert.Equal(2, result.Hops.Count);
		Assert.Equal(result.Hops.Select(hop => hop.TargetId).Distinct().Count(), result.Hops.Count);
	}

	private static Unit EnemyAt(Coord position, string id) =>
		Factory.Create(
			ShipInstance.FromCatalog(id, EType.Fighter),
			ETeam.Enemy,
			position,
			new AiController());
}
