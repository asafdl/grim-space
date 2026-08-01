using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
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

		var move = BattleTestFixture.Ui(battle).MoveUi.GetMoveOptions(battle.Sim.Actions)
			.First(option => option.EndPosition == origin + Coord.Forward * 3);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, move));

		var actions = battle.Sim.Actions.ToList();
		battle.ResolveTurn(actions);

		Assert.Equal(origin + Coord.Forward * 3, battle.Sim.StateOf<ActorState>(PlayerId).Position);
	}

	[Fact]
	public void ResolveTurnRestoresMoveHighlightsForNextPlanningTurn()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, new Coord(0, 0, 0));

		var move = BattleTestFixture.Ui(battle).MoveUi.GetMoveOptions(battle.Sim.Actions)
			.First(option => option.EndPosition == origin + Coord.Forward * 3);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, move));
		battle.ResolveTurn(battle.Sim.Actions.ToList());

		Assert.NotEmpty(BattleTestFixture.Ui(battle).MoveUi.GetMoveOptions(battle.Sim.Actions));
		Assert.False(battle.Sim.RuntimeFor(PlayerId).IsMovePathStarted);
	}

	[Fact]
	public void ResolveTurnReturnsStartAndEndSnapshots()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, new Coord(0, 0, 0));

		var move = BattleTestFixture.Ui(battle).MoveUi.GetMoveOptions(battle.Sim.Actions)
			.First(option => option.EndPosition == origin + Coord.Forward * 3);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, move));

		var replay = battle.ResolveTurn(battle.Sim.Actions.ToList());

		Assert.Equal(origin, replay.StartStates[PlayerId].Position);
		Assert.Equal(origin + Coord.Forward * 3, replay.EndStates[PlayerId].Position);
		Assert.Contains(replay.AppliedActions, action => action is MoveStepAction);
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
