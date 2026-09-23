using GrimSpace.Battle;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Abilities;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Movement;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.Actions;

[BattleTestSuite]
public sealed class RoundUpkeepTests
{
	private const string PlayerId = "player";

	[Fact]
	public void RoundUpkeepActionRefillsApAndFlak()
	{
		var player = BattleTestFixture.Player(new Coord(5, 5, 5));
		player.State.ActionPoints = 0;
		StateMountTestKit.SetUsesRemaining(player.State, GrimSpace.Units.Loadouts.Abilities.EAbilityKind.Flak, 0);
		StateMountTestKit.SetUsesRemaining(player.State, GrimSpace.Units.Loadouts.Abilities.EAbilityKind.Railgun, 0);

		ApplyRoundUpkeep(player);

		Assert.Equal(MovementExpectations.FighterApPerTurn, player.State.ActionPoints);
		Assert.Equal(CatalogExpectations.UsesPerTurn(EType.Fighter, EAbilityKind.Flak), StateMountTestKit.UsesRemaining(player.State, EAbilityKind.Flak));
		Assert.Equal(CatalogExpectations.UsesPerTurn(EType.Fighter, EAbilityKind.Railgun), StateMountTestKit.UsesRemaining(player.State, EAbilityKind.Railgun));
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
		StateMountTestKit.SetUsesRemaining(playerState, GrimSpace.Units.Loadouts.Abilities.EAbilityKind.Flak, 0);
		StateMountTestKit.SetUsesRemaining(playerState, GrimSpace.Units.Loadouts.Abilities.EAbilityKind.Railgun, 0);

		BattleTestActions.CommitAndResolve(battle);

		Assert.Equal(MovementExpectations.FighterApPerTurn, playerState.ActionPoints);
		Assert.Equal(CatalogExpectations.UsesPerTurn(EType.Fighter, EAbilityKind.Flak), StateMountTestKit.UsesRemaining(playerState, EAbilityKind.Flak));
		Assert.Equal(CatalogExpectations.UsesPerTurn(EType.Fighter, EAbilityKind.Railgun), StateMountTestKit.UsesRemaining(playerState, EAbilityKind.Railgun));
	}

	private static void ApplyRoundUpkeep(GrimSpace.Battle.Units.Unit unit)
	{
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var nonUnits = new Dictionary<string, NonUnit>();
		var engagedShipIds = new HashSet<string>(StringComparer.Ordinal) { unit.State.Id, enemy.State.Id };
		var board = BattleWorld.FromLive(
			[unit, enemy],
			nonUnits,
			BattleTestFixture.Grid(),
			new HashSet<Coord>(),
			"test-battle",
			GrimSpace.Battle.Objectives.EObjective.EliminateOpponents,
			engagedShipIds);
		var runtime = new ActorRuntime();
		BattleTestApply.TryApplyOne(
			new RoundUpkeepAction(unit.State.Id),
			board,
			runtime,
			unit.State.Id);
	}
}
