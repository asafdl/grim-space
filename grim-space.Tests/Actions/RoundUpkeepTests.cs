using GrimSpace.Battle.Board;
using GrimSpace.Battle.Turn;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Movement;

namespace GrimSpace.Tests.Actions;

public sealed class RoundUpkeepTests
{
	private const string PlayerId = "player";

	[Fact]
	public void RoundUpkeepActionRefillsApAndMissiles()
	{
		var player = BattleTestFixture.Player(new Coord(5, 5, 5));
		player.State.ActionPoints = 0;
		player.State.MissilesRemaining = 0;

		ApplyRoundUpkeep(player);

		Assert.Equal(MovementExpectations.FighterApPerTurn, player.State.ActionPoints);
		Assert.Equal(CombatConfig.MissilesPerTurn, player.State.MissilesRemaining);
	}

	[Fact]
	public void RoundUpkeepActionAppliesFlakPenaltyThenRefills()
	{
		var player = BattleTestFixture.Player(new Coord(5, 5, 5));
		player.State.ApPenaltyNextTurn = true;
		player.State.ActionPoints = 0;

		ApplyRoundUpkeep(player);

		Assert.Equal(MovementExpectations.FighterApPerTurn - 1, player.State.ActionPoints);
		Assert.False(player.State.ApPenaltyNextTurn);
	}

	[Fact]
	public void ExecuteTurnRunsRoundUpkeepOnTimeline()
	{
		var manager = TurnOrchestrationTests.CreateManager(new Coord(5, 5, 5), new Coord(0, 0, 0));
		manager.Player.BeginTurn(0);
		manager.Player.Actor.State.ActionPoints = 0;
		manager.Player.Actor.State.MissilesRemaining = 0;

		Assert.True(manager.ExecuteTurn(manager.Player.FinalizePlan()));

		Assert.Equal(MovementExpectations.FighterApPerTurn, manager.Player.Actor.State.ActionPoints);
		Assert.Equal(CombatConfig.MissilesPerTurn, manager.Player.Actor.State.MissilesRemaining);
	}

	private static void ApplyRoundUpkeep(GrimSpace.Battle.Units.Unit unit)
	{
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var nonUnits = new Dictionary<string, NonUnit>();
		var board = BattleBoard.FromLive(
			[unit, enemy],
			nonUnits,
			BattleTestFixture.Grid(),
			new HashSet<Coord>());
		var turnState = new TurnState();
		BattleTestApply.TryApplyOne(
			new RoundUpkeepAction(unit.State.Id),
			board,
			turnState,
			unit.State.Id);
	}
}
