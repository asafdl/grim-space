using GrimSpace.Battle;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Movement;
using GrimSpace.Tests.Simulation;

namespace GrimSpace.Tests.Actions;

public sealed class LegalMoveTests
{
	[Fact]
	public void EnqueueMovePathExpandsToMoveStepActions()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var planning = BattleTestFixture.BeginSimulation(player, enemy);

		var move = Preview
			.GetLegalMoves(planning)
			.First(option => option.EndPosition == origin + Coord.Forward * 3);

		Assert.True(BattleTestActions.TryEnqueueMovePath(planning, move));
		Assert.Equal(3, planning.Sim.Actions.Count);
		Assert.All(planning.Sim.Actions, action => Assert.IsType<MoveStepAction>(action));
		Assert.Equal(move.EndPosition, planning.Sim.StateOf<ActorState>(planning.PlayerId).Position);
	}

	[Fact]
	public void LegalMoveSearchFromEmptyQueueMarksAllReachableCellsAsEndable()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var planning = BattleTestFixture.BeginSimulation(player, enemy);

		var legalMoves = Preview.GetLegalMoves(planning);
		var byEnd = legalMoves.ToDictionary(option => option.EndPosition);

		Assert.Contains(origin + Coord.Forward * 3, byEnd.Keys);
		Assert.Contains(origin + Coord.Forward * 4, byEnd.Keys);
		Assert.True(byEnd.ContainsKey(origin + Coord.Forward));
		Assert.True(byEnd[origin + Coord.Forward].CanEndPath);
		Assert.True(byEnd.ContainsKey(origin + Coord.Forward * 2));
		Assert.True(byEnd[origin + Coord.Forward * 2].CanEndPath);
		Assert.True(byEnd[origin + Coord.Forward * 3].CanEndPath);
	}

	[Fact]
	public void LegalMovesShowExtensionsAfterThreeStepMove()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var planning = BattleTestFixture.BeginSimulation(player, enemy);

		var beforePlan = Preview.GetLegalMoves(planning);
		Assert.Contains(
			beforePlan,
			option => option.EndPosition == origin + Coord.Forward * 4);

		var threeStep = beforePlan.First(option => option.EndPosition == origin + Coord.Forward * 3);
		BattleTestActions.TryEnqueueMovePath(planning, threeStep);

		var afterPlan = Preview.GetLegalMoves(planning);

		Assert.Contains(afterPlan, option => option.EndPosition == origin + Coord.Forward * 4);
		Assert.Equal(
			origin + Coord.Forward * 3,
			Preview.Simulate(planning).Position);
		Assert.True(planning.Sim.TryCommit(out _, out _));
	}

	[Fact]
	public void ViewMoveHighlightsMatchLegalMoveSearch()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));

		var expected = Preview.GetLegalMoves(battle);
		var highlights = BattleTestFixture.Ui(battle).MoveUi.GetMovePaths(battle.Sim, battle.PlayerId, battle.Sim.Actions);

		Assert.Equal(
			expected.Select(option => option.EndPosition).OrderBy(coord => coord.Z),
			highlights.Select(option => option.EndPosition).OrderBy(coord => coord.Z));
	}

	[Fact]
	public void ApplyToLiveAppliesQueuedMoveActions()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var planning = BattleTestFixture.BeginSimulation(player, enemy);

		var move = Preview
			.GetLegalMoves(planning)
			.First(option => option.EndPosition == origin + Coord.Forward * 3);
		BattleTestActions.TryEnqueueMovePath(planning, move);

		var committed = planning.Sim.Actions.ToList();
		var nonUnits = new Dictionary<string, NonUnit>();

		Assert.Equal(3, committed.Count);
		Assert.All(committed, action => Assert.IsType<MoveStepAction>(action));

		BattleTestApply.ApplyToLive(
			committed,
			[player, enemy],
			planning.Layout.Grid,
			nonUnits,
			blocked,
			new Timeline(),
			planning.PlayerId);

		Assert.Equal(origin + Coord.Forward * 3, player.State.Position);
		Assert.Equal(
			MovementExpectations.MomentumAfterPureForwardPath(0, 3),
			player.State.MomentumLevel);
		Assert.Equal(
			MovementExpectations.FighterApPerTurn - move.PathApSpent,
			player.State.ActionPoints);
	}
}
