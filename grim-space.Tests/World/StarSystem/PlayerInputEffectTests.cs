using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem.Traffic;
using RunState = GrimSpace.Run.State;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem;

[StarSystemTestSuite]
public sealed class PlayerInputEffectTests(StarMapFixture maps)
{
	[Fact]
	public void Wait_BlocksCanAdvanceWhileRunning()
	{
		var orchestrator = CreatePlayerOrchestrator();
		Assert.True(orchestrator.CanAdvance);

		new PlayerInputEffect(true).Apply(orchestrator.Map, new ActorRuntime(), RunState.PlayerFleetUnitId);

		Assert.True(orchestrator.Map.WaitingForPlayerInput);
		Assert.False(orchestrator.CanAdvance);
	}

	[Fact]
	public void Resume_RestoresCanAdvanceWhileRunning()
	{
		var orchestrator = CreatePlayerOrchestrator();
		new PlayerInputEffect(true).Apply(orchestrator.Map, new ActorRuntime(), RunState.PlayerFleetUnitId);
		Assert.False(orchestrator.CanAdvance);

		new PlayerInputEffect(false).Apply(orchestrator.Map, new ActorRuntime(), RunState.PlayerFleetUnitId);

		Assert.False(orchestrator.Map.WaitingForPlayerInput);
		Assert.True(orchestrator.CanAdvance);
	}

	[Fact]
	public void Resume_KeepsSteppedMode()
	{
		var orchestrator = CreatePlayerOrchestrator();
		orchestrator.SetStepped();
		new PlayerInputEffect(true).Apply(orchestrator.Map, new ActorRuntime(), RunState.PlayerFleetUnitId);

		new PlayerInputEffect(false).Apply(orchestrator.Map, new ActorRuntime(), RunState.PlayerFleetUnitId);

		Assert.Equal(ESimMode.Stepped, orchestrator.SimMode);
	}

	[Fact]
	public void Fork_PreservesWaitingForPlayerInput()
	{
		var map = maps.Fresh(42);
		new PlayerInputEffect(true).Apply(map, new ActorRuntime(), "actor");

		var fork = map.Fork();

		Assert.True(fork.WaitingForPlayerInput);
		Assert.True(map.WaitingForPlayerInput);

		new PlayerInputEffect(false).Apply(fork, new ActorRuntime(), "actor");
		Assert.False(fork.WaitingForPlayerInput);
		Assert.True(map.WaitingForPlayerInput);
	}

	[Fact]
	public void ReachContactAction_PlayerInitiatorSetsWaitingFlag()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var pirateId = AddPirate(map, new Coord(4, 0, 0));
		new SetEngagementIntentEffect(RunState.PlayerFleetUnitId, pirateId)
			.Apply(map, new ActorRuntime(), RunState.PlayerFleetUnitId);
		new SetTravelTargetEffect(RunState.PlayerFleetUnitId, TravelTarget.Fleet(pirateId))
			.Apply(map, new ActorRuntime(), RunState.PlayerFleetUnitId);
		var engine = new Engine<StarMap, ActorRuntime>(map, new ActorRuntimes<ActorRuntime>());

		engine.Commit([new ReachContactAction(RunState.PlayerFleetUnitId, pirateId)]);

		Assert.True(map.WaitingForPlayerInput);
	}

	[Fact]
	public void ReachContactAction_NonPlayerInitiatorDoesNotSetWaitingFlag()
	{
		var map = maps.Fresh(42);
		var hunterId = AddPirate(map, new Coord(4, 0, 0), "pirate-hunter");
		var targetId = AddPirate(map, new Coord(8, 0, 0), "pirate-target");
		new SetEngagementIntentEffect(hunterId, targetId)
			.Apply(map, new ActorRuntime(), hunterId);
		new SetTravelTargetEffect(hunterId, TravelTarget.Fleet(targetId))
			.Apply(map, new ActorRuntime(), hunterId);
		var engine = new Engine<StarMap, ActorRuntime>(map, new ActorRuntimes<ActorRuntime>());

		engine.Commit([new ReachContactAction(hunterId, targetId)]);

		Assert.False(map.WaitingForPlayerInput);
	}

	[Fact]
	public void EngageAction_ClearsWaitingFlag()
	{
		var orchestrator = CreateAwaitingDecisionScenario();
		Assert.True(orchestrator.Map.WaitingForPlayerInput);

		orchestrator.PlayerAgent!.TryEnqueue([new EngageAction(RunState.PlayerFleetUnitId)]);
		orchestrator.AdvanceClock();

		Assert.False(orchestrator.Map.WaitingForPlayerInput);
	}

	[Fact]
	public void FleeAction_ClearsWaitingFlag()
	{
		var orchestrator = CreateAwaitingDecisionScenario();
		Assert.True(orchestrator.Map.WaitingForPlayerInput);

		orchestrator.PlayerAgent!.TryEnqueue([new FleeAction(RunState.PlayerFleetUnitId)]);
		orchestrator.AdvanceClock();

		Assert.False(orchestrator.Map.WaitingForPlayerInput);
	}

	[Fact]
	public void ResolveEngagementAction_DoesNotChangeWaitingFlag()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var pirateId = AddPirate(map, new Coord(4, 0, 0));
		new CommitEngagementEffect(RunState.PlayerFleetUnitId, pirateId)
			.Apply(map, new ActorRuntime(), RunState.PlayerFleetUnitId);
		new PlayerInputEffect(false).Apply(map, new ActorRuntime(), RunState.PlayerFleetUnitId);
		Assert.False(map.WaitingForPlayerInput);

		var engine = new Engine<StarMap, ActorRuntime>(map, new ActorRuntimes<ActorRuntime>());
		var outcome = new GrimSpace.Battle.Objectives.BattleOutcome(
			map.StateOf(RunState.PlayerFleetUnitId).CurrentEngagement!.Id,
			GrimSpace.Battle.Objectives.EBattleResult.Win,
			[]);
		var loot = LootCatalog.For(outcome);
		engine.Commit([new ResolveEngagementAction(
			RunState.PlayerFleetUnitId,
			outcome,
			loot.Rolls,
			loot.Total)]);

		Assert.False(map.WaitingForPlayerInput);
	}

	private StarSystemOrchestrator CreatePlayerOrchestrator()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		return StarSystemTestHarness.CreatePlayerOrchestrator(maps, RunState.PlayerFleetUnitId, 42, map: map);
	}

	private StarSystemOrchestrator CreateAwaitingDecisionScenario()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var pirateId = AddPirate(map, new Coord(4, 0, 0));
		new SetEngagementIntentEffect(RunState.PlayerFleetUnitId, pirateId)
			.Apply(map, new ActorRuntime(), RunState.PlayerFleetUnitId);
		new SetTravelTargetEffect(RunState.PlayerFleetUnitId, TravelTarget.Fleet(pirateId))
			.Apply(map, new ActorRuntime(), RunState.PlayerFleetUnitId);
		var engine = new Engine<StarMap, ActorRuntime>(map, new ActorRuntimes<ActorRuntime>());
		engine.Commit([new ReachContactAction(RunState.PlayerFleetUnitId, pirateId)]);

		return StarSystemTestHarness.CreatePlayerOrchestrator(maps, RunState.PlayerFleetUnitId, 42, map: map);
	}

	private static string AddPirate(StarMap map, Coord coord, string id = "pirate-a")
	{
		map.FleetRegistry.Add(StarSystemTestHarness.CreatePirateFleet(
			id,
			coord,
			GrimSpace.World.Factions.EFaction.Pirates,
			new GrimSpace.World.StarSystem.Encounter.CombatProfile()));
		return id;
	}
}
