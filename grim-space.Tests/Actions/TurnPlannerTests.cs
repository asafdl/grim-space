using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Movement;

namespace GrimSpace.Tests.Actions;

public sealed class TurnPlannerTests
{
	private const string PlayerId = "player";

	[Fact]
	public void TryApplyAndEnqueueRejectsIllegalActionWithoutMutatingQueue()
	{
		var origin = new Coord(5, 5, 5);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var plan = BeginPlan(origin);
		var blockedMove = new MoveStepAction(PlayerId, origin, enemy.State.Position, usedDirectionsMaskBefore: 0);

		Assert.False(plan.TryApplyAndEnqueue(blockedMove));
		Assert.Empty(plan.Actions);
		Assert.Equal(origin, plan.Board.StateOf(PlayerId).Position);
	}

	[Fact]
	public void ForceApplyAndEnqueueSkipsLegalityCheck()
	{
		var origin = new Coord(5, 5, 5);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var plan = BeginPlan(origin);
		var blockedMove = new MoveStepAction(PlayerId, origin, enemy.State.Position, usedDirectionsMaskBefore: 0);

		plan.ForceApplyAndEnqueue(blockedMove);

		Assert.Single(plan.Actions);
	}

	[Fact]
	public void CopyFromPreservesActionsButClearsBoard()
	{
		var plan = BeginPlan(new Coord(5, 5, 5));
		Assert.True(plan.TryApplyAndEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));

		var copied = new TurnPlanner();
		copied.CopyFrom(plan.Actions);

		Assert.Single(copied.Actions);
		Assert.Throws<InvalidOperationException>(() => _ = copied.Board);
	}

	[Fact]
	public void BeginTurnClearsPriorPlanAndTurnState()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var plan = new TurnPlanner();
		plan.BeginTurn(PlayerId, [player, enemy], grid, new Dictionary<string, NonUnit>(), blocked, turnStartTick: 0);

		Assert.True(plan.TryApplyAndEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.Equal(1, plan.Context.TurnState.NetYaw);

		plan.BeginTurn(PlayerId, [player, enemy], grid, new Dictionary<string, NonUnit>(), blocked, turnStartTick: 0);

		Assert.Empty(plan.Actions);
		Assert.Equal(0, plan.Context.TurnState.NetYaw);
	}

	[Theory]
	[InlineData(2, false, 1)]
	[InlineData(2, true, 2)]
	public void EndOfPhaseActionAdjustsMomentumWhenStationary(int startMomentum, bool moved, int expectedMomentum)
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin, momentum: startMomentum);
		var turnState = new TurnState();

		if (moved)
			turnState.RecordMoveStep(EStepDirection.Forward, directionBit: 1);

		var board = BattleBoard.FromSnapshot(
			[player, BattleTestFixture.Enemy(new Coord(0, 0, 0))],
			new Dictionary<string, NonUnit>(),
			BattleTestFixture.Grid(),
			new HashSet<Coord>());
		var applied = new List<IAction>();
		var context = new BattlePlanContext(applied, turnState);
		var timeline = new Timeline();

		TurnPlanner.TryApplyOne(new EndOfPhaseAction(PlayerId), board, context, timeline, PlayerId);

		Assert.Equal(expectedMomentum, board.StateOf(PlayerId).MomentumLevel);
	}

	[Fact]
	public void ReplayAppliesMomentumDecayWhenStationary()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin, momentum: 2);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var plan = new TurnPlanner();
		plan.BeginTurn(PlayerId, [player, enemy], grid, new Dictionary<string, NonUnit>(), blocked, turnStartTick: 0);

		plan.TryApplyAndEnqueue(new RollAction(PlayerId, ERollDirection.Clockwise));

		Assert.Equal(1, plan.Board.StateOf(PlayerId).MomentumLevel);
	}

	[Fact]
	public void ApplyCommittedActionMutatesLiveStateIncrementally()
	{
		var origin = new Coord(5, 5, 5);
		var startMomentum = 0;
		var player = BattleTestFixture.Player(origin, momentum: startMomentum);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var nonUnits = new Dictionary<string, NonUnit>();
		var turnState = new TurnState();
		var applied = new List<IAction>();
		var timeline = new Timeline();
		var context = new BattlePlanContext(applied, turnState);

		foreach (var step in BuildForwardSteps(origin, steps: 3, startMomentum))
		{
			TurnPlanner.ApplyCommittedAction(
				step,
				[player, enemy],
				grid,
				nonUnits,
				blocked,
				context,
				timeline,
				PlayerId);
			applied.Add(step);
		}

		Assert.Equal(origin + Coord.Forward * 3, player.State.Position);
		var expectedApCost = MovementExpectations.TotalApForPureForwardPath(startMomentum, 3);
		Assert.Equal(MovementExpectations.FighterApPerTurn - expectedApCost, player.State.ActionPoints);
	}

	[Fact]
	public void TryApplyAllStopsOnFirstIllegalAction()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var board = BattleBoard.FromSnapshot(
			[player, enemy],
			new Dictionary<string, NonUnit>(),
			grid,
			blocked);
		var turnState = new TurnState();
		var applied = new List<IAction>();
		var yaw = new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight);
		var blockedMove = new MoveStepAction(PlayerId, origin, enemy.State.Position, usedDirectionsMaskBefore: 0);
		var actions = new List<IAction> { yaw, blockedMove };
		var timeline = new Timeline();
		var context = new BattlePlanContext(applied, turnState);

		Assert.False(TurnPlanner.TryApplyAll(actions, board, context, timeline, PlayerId));
		Assert.Equal(
			MovementExpectations.FighterApPerTurn - CombatConfig.HeadingTurn90ApCost,
			board.StateOf(PlayerId).ActionPoints);
		Assert.Equal(origin, board.StateOf(PlayerId).Position);
	}

	private static TurnPlanner BeginPlan(Coord origin)
	{
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var plan = new TurnPlanner();
		plan.BeginTurn(PlayerId, [player, enemy], grid, new Dictionary<string, NonUnit>(), blocked, turnStartTick: 0);
		return plan;
	}

	private static IReadOnlyList<MoveStepAction> BuildForwardSteps(Coord origin, int steps, int startMomentum)
	{
		var option = MovementExpectations.PureForwardMove(origin, steps, startMomentum);
		var player = BattleTestFixture.Player(origin, momentum: startMomentum);
		var frame = GrimSpace.Battle.Spatial.BodyFrame.From(player.State);
		return MoveStepAction.BuildSteps(PlayerId, frame, origin, option.Path);
	}
}
