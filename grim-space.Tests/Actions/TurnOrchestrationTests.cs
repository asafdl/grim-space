using GrimSpace.Battle;
using GrimSpace.Battle.Planning;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Turn;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Actions;

public sealed class TurnOrchestrationTests
{
	[Fact]
	public void GlobalQueuePreservesPlayerThenEnemyOrder()
	{
		var origin = new Coord(5, 5, 5);
		var manager = CreateManager(origin, new Coord(0, 0, 0));
		manager.Player.BeginTurn();

		var move = Preview.GetLegalMoves(manager.Player)
			.First(option => option.EndPosition == origin + Coord.Forward * 3);
		manager.Player.TryEnqueue(new MoveAction(manager.Player.OwnerId, move));

		var commit = TurnCommit.Build(
			manager.Player.FinalizePlan(),
			manager.Units,
			manager.Grid,
			manager.Hazards.NonUnits,
			manager.Hazards.GetOccupiedCells(),
			manager.Hazards.GetBlockedCells());

		var snapshot = commit.Queue.Snapshot();
		Assert.True(snapshot.Count >= 1);
		Assert.Equal(manager.Player.OwnerId, snapshot[0].OwnerId);
		Assert.All(
			snapshot.Take(commit.PlayerPlan.Actions.Count),
			action => Assert.Equal(manager.Player.OwnerId, action.OwnerId));

		if (commit.EnemyPlan.Actions.Count > 0)
		{
			Assert.Equal(
				manager.GetEnemy()!.State.Id,
				snapshot[commit.PlayerPlan.Actions.Count].OwnerId);
		}
	}

	[Fact]
	public void ExecuteTurnSetsResolvingOnlyDuringPipeline()
	{
		using var _ = new GameLogTestScope(new TestGameLogger());

		var manager = CreateManager(new Coord(5, 5, 5), new Coord(0, 0, 0));
		Assert.False(manager.IsResolving);

		Assert.True(manager.ExecuteTurn(manager.Player.FinalizePlan()));
		Assert.False(manager.IsResolving);
	}

	private static GrimSpace.Battle.Manager CreateManager(Coord playerPos, Coord enemyPos)
	{
		var encounter = new Encounter
		{
			Seed = 1,
			Spawns =
			[
				new Spawn
				{
					Unit = new Instance
					{
						Id = "player",
						Type = EType.Fighter,
						Controller = EController.Player,
					},
					Position = playerPos,
				},
				new Spawn
				{
					Unit = new Instance
					{
						Id = "enemy",
						Type = EType.Fighter,
						Controller = EController.Enemy,
					},
					Position = enemyPos,
				},
			],
		};

		return GrimSpace.Battle.Manager.FromEncounter(encounter, gridSize: 12);
	}
}
