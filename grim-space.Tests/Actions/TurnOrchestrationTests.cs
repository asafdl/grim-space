using GrimSpace.Battle;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Actions;

public sealed class TurnOrchestrationTests
{
	private const string PlayerId = "player";

	[Fact]
	public void ResolveTurnAppliesQueuedPlayerMove()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, new Coord(0, 0, 0));

		var move = BattleTestCommands.DiscoverPaths(battle)
			.First(option => option.EndPosition == origin + Coord.Forward * 3);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, move));
		BattleTestActions.CommitAndResolve(battle);

		Assert.Equal(origin + Coord.Forward * 3, battle.Engine.World.StateOf(PlayerId).Position);
	}

	[Fact]
	public void ResolveTurnRestoresMoveHighlightsForNextPlanningTurn()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, new Coord(0, 0, 0));

		var move = BattleTestCommands.DiscoverPaths(battle)
			.First(option => option.EndPosition == origin + Coord.Forward * 3);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, move));
		BattleTestActions.CommitAndResolve(battle);
		battle.SetActive(PlayerId);

		Assert.NotEmpty(BattleTestCommands.MoveOptions(battle));
		Assert.False(battle.PlayerAgent.Sim.RuntimeFor(PlayerId).ActivePath != null);
	}

	[Fact]
	public void ResolveTurnReturnsStartAndEndSnapshots()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, new Coord(0, 0, 0));

		var move = BattleTestCommands.DiscoverPaths(battle)
			.First(option => option.EndPosition == origin + Coord.Forward * 3);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, move));

		var replay = BattleTestActions.CommitAndResolve(battle);

		Assert.Equal(origin, replay.StartStates[PlayerId].Position);
		Assert.Equal(origin + Coord.Forward * 3, replay.EndStates[PlayerId].Position);
		Assert.Contains(replay.Actions, action => action is MoveStepAction);
	}

	public static Coord EnemyInRailgunLine(Coord playerPos) => playerPos + Coord.Forward * 6;

	public static BattleOrchestrator CreateOrchestrator(Coord playerPos, Coord enemyPos)
	{
		var encounter = new Encounter
		{
			Seed = 1,
			Objective = EObjective.EliminateOpponents,
			Spawns =
			[
				new Spawn
				{
					Unit = new Instance
					{
						Id = "player",
						Type = EType.Fighter,
						Alliance = Alliance.Player,
					},
					Position = playerPos,
					ExecutionAgent = new UserExecutionAgent(),
				},
				new Spawn
				{
					Unit = new Instance
					{
						Id = "enemy",
						Type = EType.Fighter,
						Alliance = Alliance.Enemy,
					},
					Position = enemyPos,
					ExecutionAgent = new AiController(),
				},
			],
		};

		return BattleOrchestrator.FromEncounter(encounter, gridSize: 12);
	}
}
