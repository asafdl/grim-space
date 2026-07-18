using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions.Battle;
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
		var plan = new TurnPlanner();
		plan.BeginTurn(PlayerId, [player, enemy], grid, new Dictionary<string, NonUnit>(), blocked, turnStartTick: 0);

		var retro = RetroMoveOption(origin, player);
		plan.EnqueueMovePath(PlayerId, retro);
		Assert.True(plan.Context.TurnState.SpinBraked);
		Assert.True(plan.Context.TurnState.HasSpinDiscount);

		Assert.True(plan.TryApplyAndEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));

		var actor = plan.Board.StateOf(PlayerId);
		var retroApCost = MomentumConfig.ForLevel(2).BrakeCost;
		Assert.Equal(MovementExpectations.FighterApPerTurn - retroApCost, actor.ActionPoints);
		Assert.False(plan.Context.TurnState.HasSpinDiscount);
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
		var plan = new TurnPlanner();
		plan.BeginTurn(PlayerId, [player, enemy], grid, new Dictionary<string, NonUnit>(), blocked, turnStartTick: 0);

		var retro = RetroMoveOption(origin, player);
		plan.EnqueueMovePath(PlayerId, retro);
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
