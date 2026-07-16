using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Planning;
using GrimSpace.Battle.Presentation.Planning;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Movement;

namespace GrimSpace.Tests.Actions;

public sealed class PlanPreviewTests
{
	[Fact]
	public void QueuingMoveStoresItOnPlanActionList()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var planning = PlanningTestFixture.Controller(player, enemy);
		planning.ResetFrom(player.State);

		var move = Preview
			.GetSelectionMoves(planning)
			.First(option => option.EndPosition == origin + Coord.Forward * 3);

		Assert.True(planning.TryEnqueue(new MoveAction(move)));
		Assert.Single(planning.Actions);
		Assert.Equal(move.EndPosition, ((MoveAction)planning.Actions[0]).Option.EndPosition);
	}

	[Fact]
	public void LegalMovesFromEmptyPlanUseSearchAtTurnStart()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var planning = PlanningTestFixture.Controller(player, enemy);
		planning.ResetFrom(player.State);

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
		planning.ResetFrom(player.State);

		var beforePlan = Preview.GetLegalMoves(planning);
		Assert.Contains(
			beforePlan,
			option => option.EndPosition == origin + Coord.Forward * 4);

		var threeStep = beforePlan.First(option => option.EndPosition == origin + Coord.Forward * 3);
		planning.TryEnqueue(new MoveAction(threeStep));

		var afterPlan = Preview.GetLegalMoves(planning);
		var endpoints = afterPlan.Select(option => option.EndPosition).ToHashSet();

		Assert.DoesNotContain(origin + Coord.Forward * 4, endpoints);
		Assert.Equal(
			origin + Coord.Forward * 3,
			Preview.Simulate(planning).Player.Position);
	}

	[Fact]
	public void ViewMoveHighlightsExposePlanPreviewLegalMoves()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var planning = PlanningTestFixture.Controller(player, enemy);
		planning.ResetFrom(player.State);

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
		planning.ResetFrom(player.State);

		var move = Preview
			.GetSelectionMoves(planning)
			.First(option => option.EndPosition == origin + Coord.Forward * 3);
		planning.TryEnqueue(new MoveAction(move));

		var committed = planning.FinalizePlan();
		var hazards = new List<GrimSpace.Battle.Weapons.Hazard>();

		Assert.Single(committed.Actions);
		Assert.IsType<MoveAction>(committed.Actions[0]);

		BattlePlanExecutor.Apply(
			committed.Actions,
			player,
			enemy,
			planning.Grid,
			hazards,
			planning.BlockedCells,
			committed.StartFacing);

		Assert.Equal(origin + Coord.Forward * 3, player.State.Position);
		Assert.Equal(
			MovementExpectations.MomentumAfterPureForwardPath(0, 3),
			player.State.MomentumLevel);
		Assert.Equal(
			MovementExpectations.FighterApPerTurn - move.ApCost,
			player.State.ActionPoints);
	}
}
