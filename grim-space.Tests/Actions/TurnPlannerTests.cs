using GrimSpace.Battle;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
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
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(origin + Coord.Forward);
		var battle = BattleTestFixture.BeginPlanning(
			player,
			enemy,
			BattleTestFixture.Grid(),
			new HashSet<Coord> { enemy.State.Position });
		var blockedMove = new MoveStepAction(PlayerId, EStepDirection.Forward);

		Assert.False(battle.TryEnqueue(blockedMove));
		Assert.Empty(battle.Actions);
		Assert.Equal(origin, battle.Board.StateOf(PlayerId).Position);
	}

	[Fact]
	public void BeginTurnClearsPriorPlanAndPhaseContext()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var battle = BattleTestFixture.BeginPlanning(player, enemy, grid, blocked);

		Assert.True(battle.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.Equal(1, battle.Runtime.NetYaw);

		battle = BattleTestFixture.BeginPlanning(player, enemy, grid, blocked);

		Assert.Empty(battle.Actions);
		Assert.Equal(0, battle.Runtime.NetYaw);
	}

	[Theory]
	[InlineData(2, false, 1)]
	[InlineData(2, true, 2)]
	public void EndOfPhaseActionAdjustsMomentumWhenStationary(int startMomentum, bool moved, int expectedMomentum)
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin, momentum: startMomentum);
		var runtime = new ActorSession();

		if (moved)
		{
			runtime.UsedDirectionsMask = 1;
			runtime.PathForwardSteps = 1;
		}

		var board = BattleBoard.FromSnapshot(
			[player, BattleTestFixture.Enemy(new Coord(0, 0, 0))],
			new Dictionary<string, NonUnit>(),
			BattleTestFixture.Grid(),
			new HashSet<Coord>());
		BattleTestApply.TryApplyOne(new EndOfPhaseAction(PlayerId), board, runtime, PlayerId);

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
		var battle = BattleTestFixture.BeginPlanning(player, enemy, grid, blocked);

		battle.TryEnqueue(new RollAction(PlayerId, ERollDirection.Clockwise));

		Assert.Equal(1, battle.Board.StateOf(PlayerId).MomentumLevel);
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
		var runtime = new ActorSession();
		var timeline = new Timeline();

		foreach (var step in BuildForwardSteps(origin, steps: 3, startMomentum))
		{
			BattleTestApply.ApplyCommittedAction(
				step,
				[player, enemy],
				grid,
				nonUnits,
				blocked,
				runtime,
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
		var postYawForward = origin + Coord.Cross(Coord.Up, Coord.Forward);
		var enemy = BattleTestFixture.Enemy(postYawForward);
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var board = BattleBoard.FromSnapshot(
			[player, enemy],
			new Dictionary<string, NonUnit>(),
			grid,
			blocked);
		var runtime = new ActorSession();
		var yaw = new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight);
		var blockedMove = new MoveStepAction(PlayerId, EStepDirection.Forward);
		var actions = new List<IAction> { yaw, blockedMove };

		Assert.False(BattleTestApply.TryApplyAll(actions, board, runtime, PlayerId));
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
