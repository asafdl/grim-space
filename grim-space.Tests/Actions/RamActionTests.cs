using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.Actions;

[BattleTestSuite]
public sealed class RamActionTests
{
	private const string PlayerId = "player";
	private const string EnemyId = "enemy";

	[Fact]
	public void IsLegal_WhenEnemyInForwardLine()
	{
		var (_, battle) = RamBattle(enemyOffset: 3);
		var legal = Capabilities.LegalCapabilities(battle.PlayerAgent.Sim, PlayerId);

		Assert.Contains(legal, action => action is RamAction);
	}

	[Fact]
	public void IsIllegal_WhenNoEnemyInRange()
	{
		var (_, battle) = RamBattle(enemyOffset: 6);
		var legal = Capabilities.LegalCapabilities(battle.PlayerAgent.Sim, PlayerId);

		Assert.DoesNotContain(legal, action => action is RamAction);
	}

	[Fact]
	public void Resolve_DamagesEnemyAndMovesAdjacent()
	{
		var (player, battle) = RamBattle(enemyOffset: 3);
		var enemy = battle.Engine.World.UnitRegistry.UnitOf(EnemyId);
		var startHull = enemy.State.CurrentHullPoints;
		var startShields = TotalShields(enemy.State);
		var startPos = player.State.Position;
		var usesBefore = player.State.UsesRemaining(EAbilityKind.Ram);

		Assert.True(BattleTestApply.TryApplyOne(
			RamDef.Instance.Bind(PlayerId),
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(PlayerId),
			PlayerId));

		Assert.Equal(startPos + Coord.Forward * 2, player.State.Position);
		Assert.True(
			enemy.State.CurrentHullPoints < startHull || TotalShields(enemy.State) < startShields);
		Assert.Equal(usesBefore - 1, player.State.UsesRemaining(EAbilityKind.Ram));
	}

	[Fact]
	public void Resolve_EnemyAtRangeOne_StaysPutAndHits()
	{
		var (player, battle) = RamBattle(enemyOffset: 1);
		var startPos = player.State.Position;

		Assert.True(BattleTestApply.TryApplyOne(
			RamDef.Instance.Bind(PlayerId),
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(PlayerId),
			PlayerId));

		Assert.Equal(startPos, player.State.Position);
		Assert.Equal(0, player.State.UsesRemaining(EAbilityKind.Ram));
	}

	[Fact]
	public void IsIllegal_AfterUsesExhausted()
	{
		var (player, battle) = RamBattle(enemyOffset: 2);
		var world = battle.Engine.World;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);
		Assert.True(BattleTestApply.TryApplyOne(
			RamDef.Instance.Bind(PlayerId),
			world,
			runtime,
			PlayerId));

		Assert.Equal(0, player.State.UsesRemaining(EAbilityKind.Ram));
		Assert.False(RamDef.Instance.IsLegal(RamDef.Instance.Bind(PlayerId), world, runtime));
	}

	private static int TotalShields(State state) =>
		Enum.GetValues<ESpatialOrientation>().Sum(face => state.CurrentShieldPoints[face]);

	private static (Unit Player, BattleOrchestrator Battle) RamBattle(int enemyOffset)
	{
		var origin = new Coord(5, 5, 5);
		var player = Factory.Create(
			ShipInstance.FromCatalog(PlayerId, EType.IndustrialGooper),
			ETeam.Player,
			origin,
			BattleAgentFactory.Create(EBattleAgentKind.Player),
			Coord.Forward,
			Coord.Up);
		var enemy = Factory.Create(
			ShipInstance.FromCatalog(EnemyId, EType.Fighter),
			ETeam.Enemy,
			origin + Coord.Forward * enemyOffset,
			BattleAgentFactory.Create(EBattleAgentKind.Ai),
			-Coord.Forward,
			Coord.Up);
		var battle = BattleTestFixture.BeginSimulation(player, enemy);
		return (player, battle);
	}
}
