using GrimSpace.Battle;
using GrimSpace.Core;
using GrimSpace.Battle.Planning;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Turn;
using GrimSpace.Core.Log;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Actions;

public sealed class TurnOrchestrationTests
{
	[Fact]
	public void TimelinePreservesPlayerThenEnemyOrder()
	{
		var origin = new Coord(5, 5, 5);
		var manager = CreateManager(origin, new Coord(0, 0, 0));
		manager.Player.BeginTurn(0);

		var move = Preview.GetLegalMoves(manager.Player)
			.First(option => option.EndPosition == origin + Coord.Forward * 3);
		manager.Player.TryEnqueueMovePath(move);

		var commit = TurnCommit.Build(
			manager.Player.FinalizePlan(),
			manager.Timeline,
			manager.Units,
			manager.Grid,
			manager.Hazards.NonUnits,
			manager.Hazards.GetOccupiedCells(),
			manager.Hazards.GetBlockedCells());

		var playerTick = commit.TurnStart + TurnPhases.Player;
		var enemyTick = commit.TurnStart + TurnPhases.Enemy;
		var playerBucket = manager.Timeline.At(playerTick).Snapshot();
		var enemyBucket = manager.Timeline.At(enemyTick).Snapshot();

		Assert.Equal(2, playerBucket.Count);
		Assert.IsType<MovePathAction>(playerBucket[0]);
		Assert.IsType<EndOfPhaseAction>(playerBucket[^1]);
		Assert.Equal(manager.Player.OwnerId, playerBucket[0].OwnerId);

		var endTick = commit.TurnStart + TurnPhases.End;
		var endBucket = manager.Timeline.At(endTick).Snapshot();
		Assert.Equal(manager.Units.Count + 1, endBucket.Count);
		Assert.All(endBucket.Take(manager.Units.Count), action => Assert.IsType<RoundUpkeepAction>(action));
		Assert.IsType<ClearTurnHazardsAction>(endBucket[^1]);
		Assert.Equal(EntityIds.System, endBucket[^1].OwnerId);

		if (commit.EnemyActions.Count > 0)
			Assert.Equal(manager.GetEnemy()!.State.Id, enemyBucket[0].OwnerId);
	}

	[Fact]
	public void ExecuteTurnSetsResolvingOnlyDuringPipeline()
	{
		using var _ = GameLog.BeginScope(_ => { });

		var manager = CreateManager(new Coord(5, 5, 5), new Coord(0, 0, 0));
		Assert.False(manager.IsResolving);

		Assert.True(manager.ExecuteTurn(manager.Player.FinalizePlan()));
		Assert.False(manager.IsResolving);
	}

	public static GrimSpace.Battle.Manager CreateManager(Coord playerPos, Coord enemyPos)
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
