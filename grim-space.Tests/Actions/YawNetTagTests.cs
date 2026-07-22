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
		var battle = BattleTestFixture.BeginPlanning(new Coord(5, 5, 5));

		Assert.True(battle.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.True(battle.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawLeft)));

		var actor = battle.Board.StateOf(PlayerId);
		Assert.Equal(MovementExpectations.FighterApPerTurn, actor.ActionPoints);
		Assert.Equal(0, battle.Runtime.NetYaw);
	}

	[Fact]
	public void YawRightTwiceCostsTwoApForOneEighty()
	{
		var battle = BattleTestFixture.BeginPlanning(new Coord(5, 5, 5));

		Assert.True(battle.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.True(battle.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));

		var actor = battle.Board.StateOf(PlayerId);
		Assert.Equal(MovementExpectations.FighterApPerTurn - CombatConfig.HeadingTurn180ApCost, actor.ActionPoints);
		Assert.Equal(2, battle.Runtime.NetYaw);
	}

	[Fact]
	public void UndoRebuildsYawTagsFromReplay()
	{
		var battle = BattleTestFixture.BeginPlanning(new Coord(5, 5, 5));

		Assert.True(battle.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.True(battle.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawLeft)));
		Assert.Equal(MovementExpectations.FighterApPerTurn, battle.Board.StateOf(PlayerId).ActionPoints);

		Assert.True(battle.TryUndoLast());

		var actor = battle.Board.StateOf(PlayerId);
		Assert.Single(battle.Actions);
		Assert.Equal(1, battle.Runtime.NetYaw);
		Assert.Equal(MovementExpectations.FighterApPerTurn - CombatConfig.HeadingTurn90ApCost, actor.ActionPoints);
	}

	[Fact]
	public void YawRightThenLeftAtZeroMomentumDoesNotIncreaseMomentum()
	{
		var player = BattleTestFixture.Player(new Coord(5, 5, 5), momentum: 0);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var battle = BattleTestFixture.BeginPlanning(player, enemy, grid, blocked);

		Assert.True(battle.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.True(battle.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawLeft)));

		Assert.Equal(0, battle.Board.StateOf(PlayerId).MomentumLevel);
	}
}
