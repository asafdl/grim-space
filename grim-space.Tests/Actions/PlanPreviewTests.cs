using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Planning;
using GrimSpace.Battle.Presentation.Planning;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Movement;

namespace GrimSpace.Tests.Actions;

public sealed class PlanPreviewTests
{
	[Fact]
	public void QueuingMoveStoresStepActionsOnPlanActionList()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var planning = PlanningTestFixture.Controller(player, enemy);
		planning.BeginTurn(0);

		var move = Preview
			.GetLegalMoves(planning)
			.First(option => option.EndPosition == origin + Coord.Forward * 3);

		Assert.True(planning.TryEnqueueMovePath(move));
		Assert.Equal(3, planning.Actions.Count);
		Assert.All(planning.Actions, action => Assert.IsType<MoveStepAction>(action));
		Assert.Equal(move.EndPosition, ((MoveStepAction)planning.Actions[^1]).To);
		Assert.Equal(origin, ((MoveStepAction)planning.Actions[0]).From);
	}

	[Fact]
	public void LegalMovesFromEmptyPlanUseSearchAtTurnStart()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var planning = PlanningTestFixture.Controller(player, enemy);
		planning.BeginTurn(0);

		var legalMoves = Preview.GetLegalMoves(planning);
		var endpoints = legalMoves.Select(option => option.EndPosition).ToHashSet();

		Assert.Contains(origin + Coord.Forward * 3, endpoints);
		Assert.Contains(origin + Coord.Forward * 4, endpoints);
		Assert.DoesNotContain(origin + Coord.Forward, endpoints);
		Assert.DoesNotContain(origin + Coord.Forward * 2, endpoints);
	}

	[Fact]
	public void LegalMovesAfterPlannedMoveReflectRemainingApAndPosition()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var planning = PlanningTestFixture.Controller(player, enemy);
		planning.BeginTurn(0);

		var beforePlan = Preview.GetLegalMoves(planning);
		Assert.Contains(
			beforePlan,
			option => option.EndPosition == origin + Coord.Forward * 4);

		var threeStep = beforePlan.First(option => option.EndPosition == origin + Coord.Forward * 3);
		planning.TryEnqueueMovePath(threeStep);

		var afterPlan = Preview.GetLegalMoves(planning);

		Assert.Empty(afterPlan);
		Assert.Equal(
			origin + Coord.Forward * 3,
			Preview.Simulate(planning).Actor.Position);
	}

	[Fact]
	public void ViewMoveHighlightsExposePlanPreviewLegalMoves()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var planning = PlanningTestFixture.Controller(player, enemy);
		planning.BeginTurn(0);

		var expected = Preview.GetLegalMoves(planning);
		var highlights = View.GetMoveHighlights(planning, player);

		Assert.Equal(
			expected.Select(option => option.EndPosition).OrderBy(coord => coord.Z),
			highlights.Select(option => option.EndPosition).OrderBy(coord => coord.Z));
	}

	[Fact]
	public void FinalizedPlanAppliesPlanActionsOnTurnCommit()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var planning = PlanningTestFixture.Controller(player, enemy);
		planning.BeginTurn(0);

		var move = Preview
			.GetLegalMoves(planning)
			.First(option => option.EndPosition == origin + Coord.Forward * 3);
		planning.TryEnqueueMovePath(move);

		var committed = planning.FinalizePlan();
		var nonUnits = new Dictionary<string, NonUnit>();

		Assert.Equal(3, committed.Actions.Count);
		Assert.All(committed.Actions, action => Assert.IsType<MoveStepAction>(action));

		ActionApplicator.ApplyToLive(
			committed.Actions.Cast<IBattleAction>().ToList(),
			[player, enemy],
			planning.Grid,
			nonUnits,
			planning.BlockedCells,
			new Timeline(),
			planning.OwnerId);

		Assert.Equal(origin + Coord.Forward * 3, player.State.Position);
		Assert.Equal(
			MovementExpectations.MomentumAfterPureForwardPath(0, 3),
			player.State.MomentumLevel);
		Assert.Equal(
			MovementExpectations.FighterApPerTurn - move.ApCost,
			player.State.ActionPoints);
	}

	[Fact]
	public void QueuedMissilesConsumeBudgetThroughSimulation()
	{
		var origin = new Coord(5, 5, 1);
		var target = origin + Coord.Forward * CombatConfig.ForeMissileMinRange;
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var planning = PlanningTestFixture.Controller(player, enemy);
		planning.BeginTurn(0);

		var missile = new MissileAction(
			planning.OwnerId,
			target,
			EMissileMount.Fore,
			CombatConfig.ForeMissileMinRange);

		Assert.Equal(CombatConfig.MissilesPerTurn, planning.MissilesRemainingThisTurn);
		Assert.True(planning.TryEnqueue(missile));
		Assert.Equal(CombatConfig.MissilesPerTurn - 1, planning.MissilesRemainingThisTurn);
		Assert.True(planning.TryEnqueue(missile));
		Assert.Equal(0, planning.MissilesRemainingThisTurn);
		Assert.False(planning.TryEnqueue(missile));
	}
}
