using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Planning;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Turn;
using GrimSpace.Battle.Actions;
using GrimSpace.Core.Engine;
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
		var plan = TestPlan.Begin(PlayerId, origin);
		var enemyPos = enemy.State.Position;
		var frame = GrimSpace.Battle.Spatial.BodyFrame.From(plan.Board.StateOf(PlayerId));
		var direction = frame.DirectionOfStep(origin, enemyPos)
			?? EStepDirection.Forward;
		var blockedMove = new MoveStepAction(PlayerId, direction);

		Assert.False(plan.TryApplyAndEnqueue(blockedMove));
		Assert.Empty(plan.Actions);
		Assert.Equal(origin, plan.Board.StateOf(PlayerId).Position);
	}

	[Fact]
	public void ForceApplyAndEnqueueSkipsLegalityCheck()
	{
		var origin = new Coord(5, 5, 5);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var plan = TestPlan.Begin(PlayerId, origin);
		var enemyPos = enemy.State.Position;
		var frame = GrimSpace.Battle.Spatial.BodyFrame.From(plan.Board.StateOf(PlayerId));
		var direction = frame.DirectionOfStep(origin, enemyPos)
			?? EStepDirection.Forward;
		var blockedMove = new MoveStepAction(PlayerId, direction);

		plan.ForceApplyAndEnqueue(blockedMove);

		Assert.Single(plan.Actions);
	}

	[Fact]
	public void CopyFromPreservesActionsButClearsBoard()
	{
		var plan = TestPlan.Begin(PlayerId, new Coord(5, 5, 5));
		Assert.True(plan.TryApplyAndEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));

		var copied = new PlanSimulation();
		copied.CopyActionsFrom(plan.Actions);

		Assert.Single(copied.Actions);
		Assert.Null(copied.PreviewWorld);
	}

	[Fact]
	public void BeginTurnClearsPriorPlanAndPhaseContext()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var plan = TestPlan.Begin(PlayerId, player, enemy, grid, blocked);

		Assert.True(plan.TryApplyAndEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.Equal(1, plan.Context.PhaseContext.NetYaw);

		plan = TestPlan.Begin(PlayerId, player, enemy, grid, blocked);

		Assert.Empty(plan.Actions);
		Assert.Equal(0, plan.Context.PhaseContext.NetYaw);
	}

	[Theory]
	[InlineData(2, false, 1)]
	[InlineData(2, true, 2)]
	public void EndOfPhaseActionAdjustsMomentumWhenStationary(int startMomentum, bool moved, int expectedMomentum)
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin, momentum: startMomentum);
		var phaseContext = new TurnPhaseContext();

		if (moved)
		{
			phaseContext.UsedDirectionsMask = 1;
			phaseContext.PathForwardSteps = 1;
		}

		var board = BattleBoard.FromSnapshot(
			[player, BattleTestFixture.Enemy(new Coord(0, 0, 0))],
			new Dictionary<string, NonUnit>(),
			BattleTestFixture.Grid(),
			new HashSet<Coord>());
		BattleTestApply.TryApplyOne(new EndOfPhaseAction(PlayerId), board, phaseContext, PlayerId);

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
		var plan = TestPlan.Begin(PlayerId, player, enemy, grid, blocked);

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
		var phaseContext = new TurnPhaseContext();
		var timeline = new Timeline();

		foreach (var step in BuildForwardSteps(origin, steps: 3, startMomentum))
		{
			BattleTestApply.ApplyCommittedAction(
				step,
				[player, enemy],
				grid,
				nonUnits,
				blocked,
				phaseContext,
				timeline,
				PlayerId);
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
		var phaseContext = new TurnPhaseContext();
		var yaw = new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight);
		var frame = GrimSpace.Battle.Spatial.BodyFrame.From(board.StateOf(PlayerId));
		var direction = frame.DirectionOfStep(origin, enemy.State.Position)
			?? EStepDirection.Forward;
		var blockedMove = new MoveStepAction(PlayerId, direction);
		var actions = new List<IAction> { yaw, blockedMove };

		Assert.False(BattleTestApply.TryApplyAll(actions, board, phaseContext, PlayerId));
		Assert.Equal(
			MovementExpectations.FighterApPerTurn - CombatConfig.HeadingTurn90ApCost,
			board.StateOf(PlayerId).ActionPoints);
		Assert.Equal(origin, board.StateOf(PlayerId).Position);
	}

	private static IReadOnlyList<MoveStepAction> BuildForwardSteps(Coord origin, int steps, int startMomentum)
	{
		var option = MovementExpectations.PureForwardMove(origin, steps, startMomentum);
		var player = BattleTestFixture.Player(origin, momentum: startMomentum);
		var frame = GrimSpace.Battle.Spatial.BodyFrame.From(player.State);
		return MoveDef.StepsFromPath(PlayerId, frame, origin, option.Path);
	}
}
