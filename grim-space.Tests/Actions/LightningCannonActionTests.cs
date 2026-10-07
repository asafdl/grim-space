using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Actions;

[BattleTestSuite]
public sealed class LightningCannonActionTests
{
	private const string PlayerId = "player";

	private static int TotalShieldPoints(GrimSpace.Battle.Units.State state)
	{
		var total = 0;
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			total += state.ShieldPoints[face];
		return total;
	}

	[Fact]
	public void LightningCannonAppliesResolveImmediately()
	{
		var playerPos = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			playerPos, TurnOrchestrationTests.EnemyInLightningCannonLine(playerPos));
		var shieldsBefore = TotalShieldPoints(battle.PlayerAgent.Sim.StateOf<ActorState>(BattleTestFixture.FirstEnemyId(battle)));

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new LightningCannonAction(PlayerId)));
		Assert.True(shieldsBefore > TotalShieldPoints(battle.PlayerAgent.Sim.StateOf<ActorState>(BattleTestFixture.FirstEnemyId(battle))));
	}

	[Fact]
	public void LightningCannonPossibleWhenBurstMissesOpponent()
	{
		var playerPos = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(playerPos, new Coord(0, 0, 0));
		var action = new LightningCannonAction(PlayerId);

		Assert.True(LightningCannonDef.Instance.IsPossible(action, battle.PlayerAgent.Sim.World, battle.PlayerAgent.Sim.RuntimeFor(PlayerId)));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(action));
	}

	[Fact]
	public void ResolveTurnAppliesLightningCannonDamageToEnemyInBurst()
	{
		var playerPos = new Coord(5, 5, 5);
		var enemyPos = playerPos + Coord.Forward * 6;
		var battle = TurnOrchestrationTests.CreateOrchestrator(playerPos, enemyPos);
		var shieldsBefore = TotalShieldPoints(battle.PlayerAgent.Sim.StateOf<ActorState>(BattleTestFixture.FirstEnemyId(battle)));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new LightningCannonAction(PlayerId)));

		var replay = BattleTestActions.CommitAndResolve(battle);

		Assert.Contains(replay.Actions, action => action is LightningCannonAction);
		Assert.True(shieldsBefore > TotalShieldPoints(battle.Engine.World.StateOf(BattleTestFixture.FirstEnemyId(battle))));
	}

	[Fact]
	public void AsteroidBetweenShooterAndEnemyPreventsDamage()
	{
		var playerPos = new Coord(5, 5, 5);
		var enemyPos = playerPos + Coord.Forward * 6;
		var asteroidPos = playerPos + Coord.Forward * 3;
		var battle = TurnOrchestrationTests.CreateOrchestrator(playerPos, enemyPos);
		var grid = battle.Engine.World.Grid;
		var world = battle.PlayerAgent.Sim.World;
		BattleTestWorld.InjectNonUnit(
			world,
			Asteroid.Create("asteroid", asteroidPos, grid, [asteroidPos]));
		var shieldsBefore = TotalShieldPoints(battle.PlayerAgent.Sim.StateOf<ActorState>(BattleTestFixture.FirstEnemyId(battle)));

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new LightningCannonAction(PlayerId)));

		Assert.Equal(shieldsBefore, TotalShieldPoints(battle.PlayerAgent.Sim.StateOf<ActorState>(BattleTestFixture.FirstEnemyId(battle))));
	}

	[Fact]
	public void EnemyBeforeAsteroidStillTakesDamage()
	{
		var playerPos = new Coord(5, 5, 5);
		var enemyPos = playerPos + Coord.Forward * 2;
		var asteroidPos = playerPos + Coord.Forward * 4;
		var battle = TurnOrchestrationTests.CreateOrchestrator(playerPos, enemyPos);
		var grid = battle.Engine.World.Grid;
		var world = battle.PlayerAgent.Sim.World;
		BattleTestWorld.InjectNonUnit(
			world,
			Asteroid.Create("asteroid", asteroidPos, grid, [asteroidPos]));
		var shieldsBefore = TotalShieldPoints(battle.PlayerAgent.Sim.StateOf<ActorState>(BattleTestFixture.FirstEnemyId(battle)));

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new LightningCannonAction(PlayerId)));

		Assert.True(shieldsBefore > TotalShieldPoints(battle.PlayerAgent.Sim.StateOf<ActorState>(BattleTestFixture.FirstEnemyId(battle))));
	}

	[Fact]
	public void OffAxisAsteroidDoesNotPreventDamage()
	{
		var playerPos = new Coord(5, 5, 5);
		var enemyPos = playerPos + Coord.Forward * 6;
		var battle = TurnOrchestrationTests.CreateOrchestrator(playerPos, enemyPos);
		var grid = battle.Engine.World.Grid;
		var world = battle.PlayerAgent.Sim.World;
		BattleTestWorld.InjectNonUnit(
			world,
			Asteroid.Create("asteroid", playerPos + Coord.Forward * 3 + Coord.Up, grid, [playerPos + Coord.Forward * 3 + Coord.Up]));
		var shieldsBefore = TotalShieldPoints(battle.PlayerAgent.Sim.StateOf<ActorState>(BattleTestFixture.FirstEnemyId(battle)));

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new LightningCannonAction(PlayerId)));

		Assert.True(shieldsBefore > TotalShieldPoints(battle.PlayerAgent.Sim.StateOf<ActorState>(BattleTestFixture.FirstEnemyId(battle))));
	}

	[Fact]
	public void AffectedCellsExcludeAsteroidAndShadowedCells()
	{
		var playerPos = new Coord(5, 5, 5);
		var asteroidPos = playerPos + Coord.Forward * 3;
		var behind = playerPos + Coord.Forward * 6;
		var battle = TurnOrchestrationTests.CreateOrchestrator(playerPos, behind);
		var grid = battle.Engine.World.Grid;
		var world = battle.PlayerAgent.Sim.World;
		BattleTestWorld.InjectNonUnit(
			world,
			Asteroid.Create("asteroid", asteroidPos, grid, [asteroidPos]));
		var action = new LightningCannonAction(PlayerId);
		var affected = LightningCannonDef.Instance.AffectedCells(action, world);

		Assert.DoesNotContain(asteroidPos, affected);
		Assert.DoesNotContain(behind, affected);
		Assert.Contains(playerPos + Coord.Forward, affected);
	}

	[Fact]
	public void AffectedCellsReuseBlockedResultAcrossWorldForksAndInvalidateOnTopologyChange()
	{
		var playerPos = new Coord(5, 5, 5);
		var asteroidPos = playerPos + Coord.Forward * 3;
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			playerPos,
			playerPos + Coord.Forward * 6);
		var world = battle.PlayerAgent.Sim.World;
		BattleTestWorld.InjectNonUnit(
			world,
			Asteroid.Create("asteroid", asteroidPos, world.Grid, [asteroidPos]));
		var action = new LightningCannonAction(PlayerId);

		var first = LightningCannonDef.Instance.AffectedCells(action, world);
		var repeated = LightningCannonDef.Instance.AffectedCells(action, world);
		var forked = LightningCannonDef.Instance.AffectedCells(action, world.Fork());

		Assert.Same(first, repeated);
		Assert.Same(first, forked);

		Assert.True(world.RemoveNonUnit("asteroid"));
		var afterRemoval = LightningCannonDef.Instance.AffectedCells(action, world);
		Assert.NotSame(first, afterRemoval);
		Assert.Contains(asteroidPos, afterRemoval);
	}
}
