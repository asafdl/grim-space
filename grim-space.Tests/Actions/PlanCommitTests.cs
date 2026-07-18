using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Movement;

namespace GrimSpace.Tests.Actions;

public sealed class PlanCommitTests
{
	private const string PlayerId = "player";

	[Fact]
	public void FightersBeginTurnWithFourActionPoints()
	{
		var player = BattleTestFixture.Player(new Coord(5, 5, 5));

		Assert.Equal(MovementExpectations.FighterApPerTurn, player.State.ActionPoints);
	}

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
		var plan = BeginPlan(player, enemy, grid, blocked);

		var move = PlannedForwardMove(origin, stepCount, startMomentum);
		Assert.True(plan.TryApplyAndEnqueue(move));

		var preview = plan.GetPreview(PlayerId);
		var expectedApCost = MovementExpectations.TotalApForPureForwardPath(startMomentum, stepCount);
		var expectedMomentum = MovementExpectations.MomentumAfterPureForwardPath(startMomentum, stepCount);

		Assert.Equal(origin, player.State.Position);
		Assert.Equal(startMomentum, player.State.MomentumLevel);
		Assert.Equal(MovementExpectations.FighterApPerTurn, player.State.ActionPoints);

		Assert.Equal(origin + Coord.Forward * stepCount, preview.Actor.Position);
		Assert.Equal(expectedMomentum, preview.Actor.MomentumLevel);
		Assert.Equal(MovementExpectations.FighterApPerTurn - expectedApCost, preview.Actor.ActionPoints);
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
		var plan = BeginPlan(player, enemy, grid, blocked);

		var emptyPreview = plan.GetPreview(PlayerId);
		Assert.Equal(origin, emptyPreview.Actor.Position);
		Assert.Equal(MovementExpectations.FighterApPerTurn, emptyPreview.Actor.ActionPoints);

		var threeStepMove = PlannedForwardMove(origin, steps: 3, startMomentum);
		Assert.True(plan.TryApplyAndEnqueue(threeStepMove));

		var threeStepPreview = plan.GetPreview(PlayerId);
		var threeStepCost = MovementExpectations.TotalApForPureForwardPath(startMomentum, 3);
		Assert.Equal(origin + Coord.Forward * 3, threeStepPreview.Actor.Position);
		Assert.Equal(MovementExpectations.FighterApPerTurn - threeStepCost, threeStepPreview.Actor.ActionPoints);

		Assert.True(plan.TryUndoLast());
		var fourStepMove = PlannedForwardMove(origin, steps: 4, startMomentum);
		Assert.True(plan.TryApplyAndEnqueue(fourStepMove));

		var fourStepPreview = plan.GetPreview(PlayerId);
		var fourStepCost = MovementExpectations.TotalApForPureForwardPath(startMomentum, 4);
		Assert.Equal(origin + Coord.Forward * 4, fourStepPreview.Actor.Position);
		Assert.Equal(MovementExpectations.FighterApPerTurn - fourStepCost, fourStepPreview.Actor.ActionPoints);

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
		var plan = BeginPlan(player, enemy, grid, blocked);

		var move = PlannedForwardMove(origin, stepCount, startMomentum);
		var actions = new List<IAction> { move };
		var nonUnits = new Dictionary<string, NonUnit>();
		var expectedApCost = MovementExpectations.TotalApForPureForwardPath(startMomentum, stepCount);

		TurnPlanner.ApplyToLive(actions, [player, enemy], grid, nonUnits, blocked, new Timeline(), PlayerId);

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
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var plan = BeginPlan(player, enemy, grid, blocked);

		Assert.True(plan.TryApplyAndEnqueue(PlannedForwardMove(origin, steps: 3, startMomentum: 0)));
		Assert.True(plan.TryApplyAndEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));

		Assert.True(plan.TryUndoLast());
		Assert.Single(plan.Actions);
		Assert.IsType<MoveAction>(plan.Actions[0]);
	}

	[Fact]
	public void SecondMoveEnqueueRejectedUntilUndo()
	{
		var origin = new Coord(5, 5, 5);
		var planning = PlanningTestFixture.Controller(
			BattleTestFixture.Player(origin),
			BattleTestFixture.Enemy(new Coord(0, 0, 0)));
		planning.BeginTurn(0);

		var shortMove = PlannedForwardMove(origin, steps: 3, startMomentum: 0);
		var longMove = PlannedForwardMove(origin, steps: 4, startMomentum: 0);

		Assert.True(planning.TryEnqueue(shortMove));
		Assert.False(planning.TryEnqueue(longMove));
		Assert.Single(planning.Actions);

		Assert.True(planning.TryUndoLast());
		Assert.True(planning.TryEnqueue(longMove));
		Assert.Equal(
			origin + Coord.Forward * 4,
			((MoveAction)planning.Actions[0]).Option.EndPosition);
	}

	private static TurnPlanner BeginPlan(
		GrimSpace.Battle.Units.Unit player,
		GrimSpace.Battle.Units.Unit enemy,
		GrimSpace.Math.Grid.Grid grid,
		IReadOnlySet<Coord> blocked)
	{
		var plan = new TurnPlanner();
		plan.BeginTurn(PlayerId, [player, enemy], grid, new Dictionary<string, NonUnit>(), blocked, turnStartTick: 0);
		return plan;
	}

	private static MoveAction PlannedForwardMove(Coord origin, int steps, int startMomentum)
	{
		var option = MovementExpectations.PureForwardMove(origin, steps, startMomentum);
		return new MoveAction(PlayerId, new Option
		{
			Origin = origin,
			ApCost = option.ApCost,
			Path = option.Path,
		});
	}
}
