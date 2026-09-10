using GrimSpace.Core.Engine;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Narrative;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.Tests.World.StarSystem.Traffic;
using RunState = GrimSpace.Run.State;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Narrative;

public sealed class NarrativeActionTests(DevStarMapFixture maps)
{
	[Fact]
	public void BeginNarrativeAction_SetsActiveNarrativeAndWaitingFlag()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var engine = new Engine<StarMap, ActorRuntime>(map, new ActorRuntimes<ActorRuntime>());

		engine.Commit([new BeginNarrativeAction(RunState.PlayerFleetUnitId, MapNarratives.OpeningId)]);

		Assert.Equal(MapNarratives.OpeningId, map.ActiveNarrativeId);
		Assert.True(map.WaitingForPlayerInput);
	}

	[Fact]
	public void CreateDevSession_BeginsWithOpeningNarrativeActive()
	{
		var orchestrator = StarSystemOrchestrator.CreateDevSession(RunState.PlayerFleetUnitId, 42);

		Assert.Equal(MapNarratives.OpeningId, orchestrator.Map.ActiveNarrativeId);
		Assert.True(orchestrator.Map.WaitingForPlayerInput);
		Assert.False(orchestrator.CanAdvance);
	}

	[Fact]
	public void CompleteNarrativeAction_ClearsActiveNarrativeAndWaitingFlag()
	{
		var orchestrator = CreateAwaitingNarrativeScenario();
		orchestrator.PlayerAgent!.TryEnqueue([
			new CompleteNarrativeAction(RunState.PlayerFleetUnitId, MapNarratives.OpeningId),
		]);
		orchestrator.AdvanceClock();

		Assert.Null(orchestrator.Map.ActiveNarrativeId);
		Assert.False(orchestrator.Map.WaitingForPlayerInput);
		Assert.True(orchestrator.CanAdvance);
	}

	[Fact]
	public void CompleteNarrativeAction_RejectsMismatchedNarrative()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var engine = new Engine<StarMap, ActorRuntime>(map, new ActorRuntimes<ActorRuntime>());
		engine.Commit([new BeginNarrativeAction(RunState.PlayerFleetUnitId, MapNarratives.OpeningId)]);
		var sim = engine.CreateSimulation();

		Assert.False(sim.TryEnqueue(
			new CompleteNarrativeAction(RunState.PlayerFleetUnitId, "other-narrative")));
	}

	[Fact]
	public void CompleteNarrativeAction_CommitsThroughPlayerInputPath()
	{
		var orchestrator = CreateAwaitingNarrativeScenario();
		Assert.False(orchestrator.CanAdvance);

		Assert.True(orchestrator.PlayerAgent!.TryEnqueue([
			new CompleteNarrativeAction(RunState.PlayerFleetUnitId, MapNarratives.OpeningId),
		]));
		orchestrator.AdvanceClock();

		Assert.Null(orchestrator.Map.ActiveNarrativeId);
		Assert.True(orchestrator.CanAdvance);
	}

	[Fact]
	public void Fork_PreservesActiveNarrativeId()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var engine = new Engine<StarMap, ActorRuntime>(map, new ActorRuntimes<ActorRuntime>());
		engine.Commit([new BeginNarrativeAction(RunState.PlayerFleetUnitId, MapNarratives.OpeningId)]);

		var fork = map.Fork();

		Assert.Equal(MapNarratives.OpeningId, fork.ActiveNarrativeId);
		engine.Commit([new CompleteNarrativeAction(RunState.PlayerFleetUnitId, MapNarratives.OpeningId)]);
		Assert.Equal(MapNarratives.OpeningId, fork.ActiveNarrativeId);
		Assert.Null(map.ActiveNarrativeId);
	}

	private StarSystemOrchestrator CreateAwaitingNarrativeScenario()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var engine = new Engine<StarMap, ActorRuntime>(map, new ActorRuntimes<ActorRuntime>());
		engine.Commit([new BeginNarrativeAction(RunState.PlayerFleetUnitId, MapNarratives.OpeningId)]);
		return StarSystemTestHarness.CreatePlayerOrchestrator(maps, RunState.PlayerFleetUnitId, 42, map: map);
	}
}
