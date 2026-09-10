using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Actions;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Movement;
using GrimSpace.Tests.Simulation;

namespace GrimSpace.Tests.Actions;

public sealed class SimulationSessionTests
{
	private const string PlayerId = "player";

	[Fact]
	public void PlanningMoveUpdatesPreviewButNotLiveState()
	{
		var origin = new Coord(5, 5, 5);
		var startMomentum = 0;
		var stepCount = 3;
		var player = BattleTestFixture.Player(origin, momentum: startMomentum);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var battle = BeginSimulation(player, enemy, grid, blocked);

		EnqueueForwardMove(battle, steps: stepCount);

		var preview = Preview.Simulate(battle);
		var expectedApCost = MovementExpectations.TotalApForPureForwardPath(startMomentum, stepCount);

		Assert.Equal(origin, player.State.Position);
		Assert.Equal(startMomentum, player.State.MomentumLevel);
		Assert.Equal(MovementExpectations.FighterApPerTurn, player.State.ActionPoints);

		Assert.Equal(origin + Coord.Forward * stepCount, preview.Position);
		Assert.Equal(startMomentum, preview.MomentumLevel);
		Assert.Equal(MovementExpectations.FighterApPerTurn - stepCount, preview.ActionPoints);
	}

	[Fact]
	public void PreviewReflectsQueuedMoveAndUpdatesAfterUndoAndReplace()
	{
		var origin = new Coord(5, 5, 5);
		var startMomentum = 0;
		var player = BattleTestFixture.Player(origin, momentum: startMomentum);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var battle = BeginSimulation(player, enemy, grid, blocked);

		var emptyPreview = Preview.Simulate(battle);
		Assert.Equal(origin, emptyPreview.Position);
		Assert.Equal(MovementExpectations.FighterApPerTurn, emptyPreview.ActionPoints);

		EnqueueForwardMove(battle, steps: 3);
		var threeStepPreview = Preview.Simulate(battle);
		Assert.Equal(origin + Coord.Forward * 3, threeStepPreview.Position);
		Assert.Equal(MovementExpectations.FighterApPerTurn - 3, threeStepPreview.ActionPoints);

		Assert.True(battle.PlayerAgent.Sim.TryUndoLast());
		EnqueueForwardMove(battle, steps: 4);

		var fourStepPreview = Preview.Simulate(battle);
		Assert.Equal(origin + Coord.Forward * 4, fourStepPreview.Position);
		Assert.Equal(0, fourStepPreview.ActionPoints);

		Assert.Equal(origin, player.State.Position);
		Assert.Equal(MovementExpectations.FighterApPerTurn, player.State.ActionPoints);
	}

	[Fact]
	public void UndoLastRemovesCombinedMoveSegmentAsOneGroup()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var battle = BeginSimulation(player, enemy, grid, blocked);

		var path = MovePathEndpoints.DiscoverExtensions(battle.PlayerAgent.Sim, battle.PlayerId)
			.First(option => option.EndPosition == origin + Coord.Forward * 3);
		Assert.True(battle.PlayerAgent.TryEnqueue(path.Steps.Cast<Core.Actions.IAction>().ToList()));

		Assert.True(battle.PlayerAgent.Sim.TryUndoLast());
		Assert.Empty(battle.PlayerAgent.Sim.Actions);
	}

	private static BattleOrchestrator BeginSimulation(
		GrimSpace.Battle.Units.Unit player,
		GrimSpace.Battle.Units.Unit enemy,
		GrimSpace.Math.Grid.Grid grid,
		IReadOnlySet<Coord> blocked) =>
		BattleTestFixture.BeginSimulation(player, enemy, grid, blocked);

	private static void EnqueueForwardMove(BattleOrchestrator battle, int steps)
	{
		var origin = battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).Position;
		var end = origin + Coord.Forward * steps;
		var path = MovePathEndpoints.DiscoverExtensions(battle.PlayerAgent.Sim, battle.PlayerId)
			.First(p => p.EndPosition == end);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, path));
	}
}
