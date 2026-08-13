using GrimSpace.Battle.World;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Abilities;
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
		var battle = BattleTestFixture.BeginSimulation(player, enemy, grid, blocked);

		var retro = RetroMoveOption(origin, player);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, retro));
		Assert.True(battle.PlayerAgent.Sim.RuntimeFor(PlayerId).SpinBraked);
		Assert.True(battle.PlayerAgent.Sim.RuntimeFor(PlayerId).SpinDiscount);

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));

		var actor = battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId);
		var retroApCost = MomentumConfig.ForLevel(2).BrakeCost;
		Assert.Equal(MovementExpectations.FighterApPerTurn - retroApCost, actor.ActionPoints);
		Assert.False(battle.PlayerAgent.Sim.RuntimeFor(PlayerId).SpinDiscount);
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
		var battle = BattleTestFixture.BeginSimulation(player, enemy, grid, blocked);

		var retro = RetroMoveOption(origin, player);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, retro));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));

		var actor = battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId);
		var retroApCost = MomentumConfig.ForLevel(2).BrakeCost;
		Assert.Equal(
			MovementExpectations.FighterApPerTurn - retroApCost - CombatConfig.HeadingTurn90ApCost,
			actor.ActionPoints);
		Assert.Equal(0, actor.MomentumLevel);
	}

	private static MovePathSession RetroMoveOption(Coord origin, GrimSpace.Battle.Units.Unit player) =>
		BattleTestFixture.Path(
			PlayerId,
			origin,
			pathApSpent: 0,
			Coord.Zero - player.State.Fore);
}
