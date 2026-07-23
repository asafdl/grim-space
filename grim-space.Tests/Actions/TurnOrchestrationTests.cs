using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation.Planning;
using GrimSpace.Battle.Turn;
using GrimSpace.Core;
using GrimSpace.Core.Log;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Actions;

public sealed class TurnOrchestrationTests
{
	[Fact]
	public void ResolveTurnAppliesPlayerMoveBeforeRoundUpkeep()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, new Coord(0, 0, 0));

		var move = View.GetLegalMoves(battle)
			.First(option => option.EndPosition == origin + Coord.Forward * 3);
		Assert.True(battle.TryEnqueueMovePath(move));

		var actions = battle.Actions.ToList();
		Assert.True(battle.ResolveTurn(actions));

		Assert.Equal(origin + Coord.Forward * 3, battle.Board.StateOf(battle.PlayerId).Position);
	}

	[Fact]
	public void ResolveTurnSetsResolvingOnlyDuringPipeline()
	{
		using var _ = GameLog.BeginScope(_ => { });

		var battle = CreateOrchestrator(new Coord(5, 5, 5), new Coord(0, 0, 0));
		Assert.False(battle.IsResolving);

		Assert.True(battle.ResolveTurn([]));
		Assert.False(battle.IsResolving);
	}

	public static BattleOrchestrator CreateOrchestrator(Coord playerPos, Coord enemyPos)
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

		return BattleOrchestrator.FromEncounter(encounter, gridSize: 12);
	}
}
