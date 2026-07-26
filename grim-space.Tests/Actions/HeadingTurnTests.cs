using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Weapons;
using GrimSpace.Battle.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Movement;

namespace GrimSpace.Tests.Actions;

public sealed class HeadingTurnTests
{
	private const string PlayerId = "player";

	[Fact]
	public void YawRightThenLeftCostsApPerAction()
	{
		var battle = BattleTestFixture.BeginPlanning(new Coord(5, 5, 5));

		Assert.True(battle.Sim.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.True(battle.Sim.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawLeft)));

		var actor = battle.Sim.StateOf<ActorState>(PlayerId);
		Assert.Equal(
			MovementExpectations.FighterApPerTurn - CombatConfig.HeadingTurn90ApCost * 2,
			actor.ActionPoints);
		Assert.Equal(0, battle.Sim.RuntimeFor(PlayerId).NetYaw);
	}

	[Fact]
	public void YawRightTwiceCostsStickerApPerTurn()
	{
		var battle = BattleTestFixture.BeginPlanning(new Coord(5, 5, 5));

		Assert.True(battle.Sim.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.True(battle.Sim.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));

		var actor = battle.Sim.StateOf<ActorState>(PlayerId);
		Assert.Equal(
			MovementExpectations.FighterApPerTurn - CombatConfig.HeadingTurn90ApCost * 2,
			actor.ActionPoints);
		Assert.Equal(2, battle.Sim.RuntimeFor(PlayerId).NetYaw);
	}

	[Fact]
	public void UndoRebuildsYawQuartersFromReplay()
	{
		var battle = BattleTestFixture.BeginPlanning(new Coord(5, 5, 5));

		Assert.True(battle.Sim.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.True(battle.Sim.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawLeft)));
		Assert.Equal(
			MovementExpectations.FighterApPerTurn - CombatConfig.HeadingTurn90ApCost * 2,
			battle.Sim.StateOf<ActorState>(PlayerId).ActionPoints);

		Assert.True(battle.Sim.TryUndoLast());

		var actor = battle.Sim.StateOf<ActorState>(PlayerId);
		Assert.Single(battle.Sim.Actions);
		Assert.Equal(1, battle.Sim.RuntimeFor(PlayerId).NetYaw);
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

		Assert.True(battle.Sim.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.True(battle.Sim.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawLeft)));

		Assert.Equal(0, battle.Sim.StateOf<ActorState>(PlayerId).MomentumLevel);
	}
}
