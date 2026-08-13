using GrimSpace.Battle;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Abilities;
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
	public void RoundUpkeepActionRefillsApAndFlak()
	{
		var player = BattleTestFixture.Player(new Coord(5, 5, 5));
		player.State.ActionPoints = 0;
		player.State.FlakRemaining = 0;
		player.State.RailgunRemaining = 0;

		ApplyRoundUpkeep(player);

		Assert.Equal(MovementExpectations.FighterApPerTurn, player.State.ActionPoints);
		Assert.Equal(CombatConfig.FlaksPerTurn, player.State.FlakRemaining);
		Assert.Equal(CombatConfig.RailgunsPerTurn, player.State.RailgunRemaining);
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
	public void ResolveTurnRunsRoundUpkeepOnTimeline()
	{
		var battle = TurnOrchestrationTests.CreateOrchestrator(new Coord(5, 5, 5), new Coord(0, 0, 0));
		var playerState = battle.Engine.World.StateOf(battle.PlayerId);
		playerState.ActionPoints = 0;
		playerState.FlakRemaining = 0;
		playerState.RailgunRemaining = 0;

		BattleTestActions.CommitAndResolve(battle);

		Assert.Equal(MovementExpectations.FighterApPerTurn, playerState.ActionPoints);
		Assert.Equal(CombatConfig.FlaksPerTurn, playerState.FlakRemaining);
		Assert.Equal(CombatConfig.RailgunsPerTurn, playerState.RailgunRemaining);
	}

	private static void ApplyRoundUpkeep(GrimSpace.Battle.Units.Unit unit)
	{
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var nonUnits = new Dictionary<string, NonUnit>();
		var board = BattleWorld.FromLive(
			[unit, enemy],
			nonUnits,
			BattleTestFixture.Grid(),
			new HashSet<Coord>());
		var runtime = new ActorRuntime();
		BattleTestApply.TryApplyOne(
			new RoundUpkeepAction(unit.State.Id),
			board,
			runtime,
			unit.State.Id);
	}
}
