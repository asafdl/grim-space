using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Weapons;
using GrimSpace.Battle.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Movement;

namespace GrimSpace.Tests.Actions;

public sealed class ActionTagSynergyTests
{
	private const string PlayerId = "player";

	[Fact]
	public void RetroMoveThenFirstYawGetsSpinDiscount()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin, momentum: 2);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var plan = TestPlan.Begin(PlayerId, player, enemy, grid, blocked);

		var retro = RetroMoveOption(origin, player);
		plan.EnqueueMovePath(retro);
		Assert.True(plan.Runtime.SpinBraked);
		Assert.True(plan.Runtime.SpinDiscount);

		Assert.True(plan.TryApplyAndEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));

		var actor = plan.Board.StateOf(PlayerId);
		var retroApCost = MomentumConfig.ForLevel(2).BrakeCost;
		Assert.Equal(MovementExpectations.FighterApPerTurn - retroApCost, actor.ActionPoints);
		Assert.False(plan.Runtime.SpinDiscount);
		Assert.Equal(1, actor.MomentumLevel);
	}

	[Fact]
	public void RetroMoveThenSecondYawPaysFullCost()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin, momentum: 2);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var plan = TestPlan.Begin(PlayerId, player, enemy, grid, blocked);

		var retro = RetroMoveOption(origin, player);
		plan.EnqueueMovePath(retro);
		Assert.True(plan.TryApplyAndEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.True(plan.TryApplyAndEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));

		var actor = plan.Board.StateOf(PlayerId);
		var retroApCost = MomentumConfig.ForLevel(2).BrakeCost;
		Assert.Equal(
			MovementExpectations.FighterApPerTurn - retroApCost - CombatConfig.HeadingTurn90ApCost,
			actor.ActionPoints);
		Assert.Equal(0, actor.MomentumLevel);
	}

	private static Option RetroMoveOption(Coord origin, GrimSpace.Battle.Units.Unit player)
	{
		var retroPath = BattleTestFixture.Path(
			origin,
			apCost: 0,
			Coord.Zero - player.State.Fore);

		return retroPath;
	}
}
