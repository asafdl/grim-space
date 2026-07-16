using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Movement;

namespace GrimSpace.Tests.Actions;

public sealed class PlanCommitTests
{
	[Fact]
	public void FightersBeginTurnWithFourActionPoints()
	{
		var player = BattleTestFixture.Player(new Coord(5, 5, 5));

		Assert.Equal(MovementExpectations.FighterApPerTurn, player.State.ActionPoints);
	}

	[Fact]
	public void PlanningMoveUpdatesPreviewPositionAndApButNotMomentumOrLiveState()
	{
		var origin = new Coord(5, 5, 5);
		var startMomentum = 0;
		var stepCount = 3;
		var player = BattleTestFixture.Player(origin, momentum: startMomentum);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var plan = new PlayerPlan();
		plan.ResetFrom(player.State);

		var move = PlannedForwardMove(origin, stepCount, startMomentum);
		plan.Enqueue(move);

		var preview = BattlePlanExecutor.Simulate(player, enemy, grid, plan, blocked);
		var expectedApCost = MovementExpectations.TotalApForPureForwardPath(startMomentum, stepCount);

		Assert.Equal(origin, player.State.Position);
		Assert.Equal(startMomentum, player.State.MomentumLevel);
		Assert.Equal(MovementExpectations.FighterApPerTurn, player.State.ActionPoints);

		Assert.Equal(origin + Coord.Forward * stepCount, preview.Player.Position);
		Assert.Equal(startMomentum, preview.Player.MomentumLevel);
		Assert.Equal(MovementExpectations.FighterApPerTurn - expectedApCost, preview.Player.ActionPoints);
	}

	[Fact]
	public void PreviewReflectsQueuedMoveAndUpdatesWhenPlanChanges()
	{
		var origin = new Coord(5, 5, 5);
		var startMomentum = 0;
		var player = BattleTestFixture.Player(origin, momentum: startMomentum);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var plan = new PlayerPlan();
		plan.ResetFrom(player.State);

		var emptyPreview = BattlePlanExecutor.Simulate(player, enemy, grid, plan, blocked);
		Assert.Equal(origin, emptyPreview.Player.Position);
		Assert.Equal(MovementExpectations.FighterApPerTurn, emptyPreview.Player.ActionPoints);

		var threeStepMove = PlannedForwardMove(origin, steps: 3, startMomentum);
		plan.Enqueue(threeStepMove);

		var threeStepPreview = BattlePlanExecutor.Simulate(player, enemy, grid, plan, blocked);
		var threeStepCost = MovementExpectations.TotalApForPureForwardPath(startMomentum, 3);
		Assert.Equal(origin + Coord.Forward * 3, threeStepPreview.Player.Position);
		Assert.Equal(MovementExpectations.FighterApPerTurn - threeStepCost, threeStepPreview.Player.ActionPoints);

		var fourStepMove = PlannedForwardMove(origin, steps: 4, startMomentum);
		plan.Enqueue(fourStepMove);

		var fourStepPreview = BattlePlanExecutor.Simulate(player, enemy, grid, plan, blocked);
		var fourStepCost = MovementExpectations.TotalApForPureForwardPath(startMomentum, 4);
		Assert.Equal(origin + Coord.Forward * 4, fourStepPreview.Player.Position);
		Assert.Equal(MovementExpectations.FighterApPerTurn - fourStepCost, fourStepPreview.Player.ActionPoints);

		Assert.Equal(origin, player.State.Position);
		Assert.Equal(MovementExpectations.FighterApPerTurn, player.State.ActionPoints);
	}

	[Fact]
	public void EndingTurnAppliesMovePositionMomentumAndAp()
	{
		var origin = new Coord(5, 5, 5);
		var startMomentum = 0;
		var stepCount = 3;
		var player = BattleTestFixture.Player(origin, momentum: startMomentum);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var plan = new PlayerPlan();
		plan.ResetFrom(player.State);

		var move = PlannedForwardMove(origin, stepCount, startMomentum);
		var actions = new List<IBattleAction> { move };
		var hazards = new List<Hazard>();
		var expectedApCost = MovementExpectations.TotalApForPureForwardPath(startMomentum, stepCount);

		BattlePlanExecutor.Apply(actions, player, enemy, grid, hazards, blocked, plan);

		Assert.Equal(origin + Coord.Forward * stepCount, player.State.Position);
		Assert.Equal(
			MovementExpectations.MomentumAfterPureForwardPath(startMomentum, stepCount),
			player.State.MomentumLevel);
		Assert.Equal(MovementExpectations.FighterApPerTurn - expectedApCost, player.State.ActionPoints);
	}

	[Fact]
	public void UndoRemovesMostRecentlyQueuedAction()
	{
		var origin = new Coord(5, 5, 5);
		var plan = new PlayerPlan();
		plan.ResetFrom(BattleTestFixture.Player(origin).State);

		plan.Enqueue(PlannedForwardMove(origin, steps: 3, startMomentum: 0));
		plan.Enqueue(new HeadingTurnAction(EHeadingTurn.YawRight));

		Assert.True(plan.TryUndoLast());
		Assert.Single(plan.Actions);
		Assert.IsType<MoveAction>(plan.Actions[0]);
	}

	[Fact]
	public void QueueingNewMoveReplacesPriorQueuedMove()
	{
		var origin = new Coord(5, 5, 5);
		var plan = new PlayerPlan();
		plan.ResetFrom(BattleTestFixture.Player(origin).State);

		var shortMove = PlannedForwardMove(origin, steps: 3, startMomentum: 0);
		var longMove = PlannedForwardMove(origin, steps: 4, startMomentum: 0);

		plan.Enqueue(shortMove);
		plan.Enqueue(longMove);

		Assert.Single(plan.Actions);
		Assert.Equal(longMove.Option.EndPosition, ((MoveAction)plan.Actions[0]).Option.EndPosition);
	}

	private static MoveAction PlannedForwardMove(Coord origin, int steps, int startMomentum) =>
		new(MovementExpectations.PureForwardMove(origin, steps, startMomentum));
}
