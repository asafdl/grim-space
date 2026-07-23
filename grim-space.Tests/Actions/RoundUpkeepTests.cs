using GrimSpace.Battle;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Movement;

namespace GrimSpace.Tests.Actions;

public sealed class RoundUpkeepTests
{
	private const string PlayerId = "player";

	[Fact]
	public void RoundUpkeepActionRefillsApMissilesAndFlak()
	{
		var player = BattleTestFixture.Player(new Coord(5, 5, 5));
		player.State.ActionPoints = 0;
		player.State.MissilesRemaining = 0;
		player.State.FlakRemaining = 0;

		ApplyRoundUpkeep(player);

		Assert.Equal(MovementExpectations.FighterApPerTurn, player.State.ActionPoints);
		Assert.Equal(CombatConfig.MissilesPerTurn, player.State.MissilesRemaining);
		Assert.Equal(CombatConfig.FlaksPerTurn, player.State.FlakRemaining);
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
		var battle = TurnOrchestrationTests.CreateOrchestrator(new Coord(5, 5, 5), new Coord(0, 0, 0));
		var player = battle.GetPlayer()!;
		player.State.ActionPoints = 0;
		player.State.MissilesRemaining = 0;
		player.State.FlakRemaining = 0;

		Assert.True(battle.ResolveTurn([]));

		Assert.Equal(MovementExpectations.FighterApPerTurn, player.State.ActionPoints);
		Assert.Equal(CombatConfig.MissilesPerTurn, player.State.MissilesRemaining);
		Assert.Equal(CombatConfig.FlaksPerTurn, player.State.FlakRemaining);
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
		var runtime = new ActorSession();
		BattleTestApply.TryApplyOne(
			new RoundUpkeepAction(unit.State.Id),
			board,
			runtime,
			unit.State.Id);
	}
}
