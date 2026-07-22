using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Weapons;
using GrimSpace.Battle.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Movement;

namespace GrimSpace.Tests.Actions;

public sealed class YawNetTagTests
{
	private const string PlayerId = "player";

	[Fact]
	public void YawRightThenLeftCostsZeroAp()
	{
		var plan = TestPlan.Begin(PlayerId, new Coord(5, 5, 5));

		Assert.True(plan.TryApplyAndEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.True(plan.TryApplyAndEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawLeft)));

		var actor = plan.Board.StateOf(PlayerId);
		Assert.Equal(MovementExpectations.FighterApPerTurn, actor.ActionPoints);
		Assert.Equal(0, plan.Runtime.NetYaw);
	}

	[Fact]
	public void YawRightTwiceCostsTwoApForOneEighty()
	{
		var plan = TestPlan.Begin(PlayerId, new Coord(5, 5, 5));

		Assert.True(plan.TryApplyAndEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.True(plan.TryApplyAndEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));

		var actor = plan.Board.StateOf(PlayerId);
		Assert.Equal(MovementExpectations.FighterApPerTurn - CombatConfig.HeadingTurn180ApCost, actor.ActionPoints);
		Assert.Equal(2, plan.Runtime.NetYaw);
	}

	[Fact]
	public void UndoRebuildsYawTagsFromReplay()
	{
		var plan = TestPlan.Begin(PlayerId, new Coord(5, 5, 5));

		Assert.True(plan.TryApplyAndEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.True(plan.TryApplyAndEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawLeft)));
		Assert.Equal(MovementExpectations.FighterApPerTurn, plan.Board.StateOf(PlayerId).ActionPoints);

		Assert.True(plan.TryUndoLast());

		var actor = plan.Board.StateOf(PlayerId);
		Assert.Single(plan.Actions);
		Assert.Equal(1, plan.Runtime.NetYaw);
		Assert.Equal(MovementExpectations.FighterApPerTurn - CombatConfig.HeadingTurn90ApCost, actor.ActionPoints);
	}

	[Fact]
	public void YawRightThenLeftAtZeroMomentumDoesNotIncreaseMomentum()
	{
		var player = BattleTestFixture.Player(new Coord(5, 5, 5), momentum: 0);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var plan = TestPlan.Begin(PlayerId, player, enemy, grid, blocked);

		Assert.True(plan.TryApplyAndEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.True(plan.TryApplyAndEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawLeft)));

		Assert.Equal(0, plan.Board.StateOf(PlayerId).MomentumLevel);
	}
}
