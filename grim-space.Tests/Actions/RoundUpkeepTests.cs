using GrimSpace.Battle;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Effects;
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
	public void RoundUpkeepActionRefillsResourcesAndScrapDroneSwarm()
	{
		var player = BattleTestFixture.Player(new Coord(5, 5, 5));
		player.State.ActionPoints = 0;
		player.State.ManeuverPoints = 0;
		StateMountTestKit.SetUsesRemaining(player.State, GrimSpace.Units.Loadouts.Abilities.EAbilityKind.ScrapDroneSwarm, 0);
		StateMountTestKit.SetUsesRemaining(player.State, GrimSpace.Units.Loadouts.Abilities.EAbilityKind.LightningCannon, 0);

		ApplyRoundUpkeep(player);

		Assert.Equal(MovementExpectations.FighterApPerTurn, player.State.ActionPoints);
		Assert.Equal(3, player.State.ManeuverPoints);
		Assert.Equal(CatalogExpectations.UsesPerTurn(EType.Fighter, EAbilityKind.ScrapDroneSwarm), StateMountTestKit.UsesRemaining(player.State, EAbilityKind.ScrapDroneSwarm));
		Assert.Equal(CatalogExpectations.UsesPerTurn(EType.Fighter, EAbilityKind.LightningCannon), StateMountTestKit.UsesRemaining(player.State, EAbilityKind.LightningCannon));
	}

	[Fact]
	public void RoundUpkeepActionAppliesScrapDroneSwarmPenaltyThenRefills()
	{
		var player = BattleTestFixture.Player(new Coord(5, 5, 5));
		player.State.ApPenaltyNextTurn = true;
		player.State.ActionPoints = 0;
		player.State.ManeuverPoints = 0;

		ApplyRoundUpkeep(player);

		Assert.Equal(MovementExpectations.FighterApPerTurn - 1, player.State.ActionPoints);
		Assert.Equal(3, player.State.ManeuverPoints);
		Assert.False(player.State.ApPenaltyNextTurn);
	}

	[Fact]
	public void ResolveTurnRunsRoundUpkeepOnTimeline()
	{
		var battle = TurnOrchestrationTests.CreateOrchestrator(new Coord(5, 5, 5), new Coord(0, 0, 0));
		var playerState = battle.Engine.World.StateOf(battle.PlayerId);
		playerState.ActionPoints = 0;
		playerState.ManeuverPoints = 0;
		StateMountTestKit.SetUsesRemaining(playerState, GrimSpace.Units.Loadouts.Abilities.EAbilityKind.ScrapDroneSwarm, 0);
		StateMountTestKit.SetUsesRemaining(playerState, GrimSpace.Units.Loadouts.Abilities.EAbilityKind.LightningCannon, 0);

		BattleTestActions.CommitAndResolve(battle);

		Assert.Equal(MovementExpectations.FighterApPerTurn, playerState.ActionPoints);
		Assert.Equal(3, playerState.ManeuverPoints);
		Assert.Equal(CatalogExpectations.UsesPerTurn(EType.Fighter, EAbilityKind.ScrapDroneSwarm), StateMountTestKit.UsesRemaining(playerState, EAbilityKind.ScrapDroneSwarm));
		Assert.Equal(CatalogExpectations.UsesPerTurn(EType.Fighter, EAbilityKind.LightningCannon), StateMountTestKit.UsesRemaining(playerState, EAbilityKind.LightningCannon));
	}

	[Fact]
	public void RoundUpkeepUndoRestoresBothResourcePools()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.ActionPoints = 1;
		state.ManeuverPoints = 0;
		state.ApPenaltyNextTurn = true;
		var effect = new RoundUpkeepEffect();
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);

		effect.Apply(battle.Engine.World, runtime, PlayerId);
		effect.Undo(battle.Engine.World, runtime, PlayerId);

		Assert.Equal(1, state.ActionPoints);
		Assert.Equal(0, state.ManeuverPoints);
		Assert.True(state.ApPenaltyNextTurn);
	}

	[Fact]
	public void ManeuverPointChangeUndoRestoresPreviousValue()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		var effect = new MpChangeEffect(-2);
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);

		effect.Apply(battle.Engine.World, runtime, PlayerId);
		Assert.Equal(1, state.ManeuverPoints);

		effect.Undo(battle.Engine.World, runtime, PlayerId);
		Assert.Equal(3, state.ManeuverPoints);
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
