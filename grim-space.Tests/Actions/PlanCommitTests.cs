using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Planning;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
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
		var plan = BeginPlanning(player, enemy, grid, blocked);
		plan.BeginTurn(0);

		EnqueueForwardMove(plan, origin, stepCount, startMomentum);

		var preview = Preview.Simulate(plan);
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
		var plan = BeginPlanning(player, enemy, grid, blocked);

		var emptyPreview = Preview.Simulate(plan);
		Assert.Equal(origin, emptyPreview.Actor.Position);
		Assert.Equal(MovementExpectations.FighterApPerTurn, emptyPreview.Actor.ActionPoints);

		EnqueueForwardMove(plan, origin, steps: 3, startMomentum);
		var threeStepPreview = Preview.Simulate(plan);
		var threeStepCost = MovementExpectations.TotalApForPureForwardPath(startMomentum, 3);
		Assert.Equal(origin + Coord.Forward * 3, threeStepPreview.Actor.Position);
		Assert.Equal(MovementExpectations.FighterApPerTurn - threeStepCost, threeStepPreview.Actor.ActionPoints);

		Assert.True(plan.TryUndoLast());
		EnqueueForwardMove(plan, origin, steps: 4, startMomentum);

		var fourStepPreview = Preview.Simulate(plan);
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
		var plan = BeginPlanning(player, enemy, grid, blocked);
		plan.BeginTurn(0);

		EnqueueForwardMove(plan, origin, stepCount, startMomentum);
		var actions = plan.Actions.ToList();
		var nonUnits = new Dictionary<string, NonUnit>();
		var expectedApCost = MovementExpectations.TotalApForPureForwardPath(startMomentum, stepCount);

		BattleTestApply.ApplyToLive(actions, [player, enemy], grid, nonUnits, blocked, new Timeline(), PlayerId);

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
		var plan = BeginPlanning(player, enemy, grid, blocked);

		EnqueueForwardMove(plan, origin, steps: 3, startMomentum: 0);
		Assert.True(plan.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));

		Assert.True(plan.TryUndoLast());
		Assert.Equal(3, plan.Actions.Count);
		Assert.All(plan.Actions, action => Assert.IsType<MoveStepAction>(action));
	}

	[Fact]
	public void SecondMoveEnqueueRejectedUntilUndo()
	{
		var origin = new Coord(5, 5, 5);
		var planning = PlanningTestFixture.Controller(
			BattleTestFixture.Player(origin),
			BattleTestFixture.Enemy(new Coord(0, 0, 0)));
		planning.BeginTurn(0);

		var shortMove = MovementExpectations.PureForwardMove(origin, stepCount: 3, startMomentum: 0);
		var longMove = MovementExpectations.PureForwardMove(origin, stepCount: 4, startMomentum: 0);

		Assert.True(planning.TryEnqueueMovePath(shortMove));
		Assert.False(planning.TryEnqueueMovePath(longMove));
		Assert.Equal(3, planning.Actions.Count);

		Assert.True(planning.TryUndoLast());
		Assert.True(planning.TryEnqueueMovePath(longMove));
		Assert.Equal(
			origin + Coord.Forward * 4,
			planning.Board.StateOf(planning.OwnerId).Position);
	}

	private static TestPlan BeginPlanning(
		GrimSpace.Battle.Units.Unit player,
		GrimSpace.Battle.Units.Unit enemy,
		GrimSpace.Math.Grid.Grid grid,
		IReadOnlySet<Coord> blocked) =>
		PlanningTestFixture.Controller(player, enemy, grid, blocked);

	private static void EnqueueForwardMove(TestPlan plan, Coord origin, int steps, int startMomentum)
	{
		var option = MovePathFinder.Find(plan.Board, plan.Runtime, plan.OwnerId)
			.First(o => o.Path.Count == steps);
		Assert.True(plan.TryEnqueueMovePath(option));
	}
}
