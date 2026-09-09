using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.Tests.World.StarSystem.Traffic;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

public sealed class AcceptContractActionTests(DevStarMapFixture maps)
{
	[Fact]
	public void TryEnqueue_SucceedsForOfferedContract()
	{
		var (engine, unitId, contractId) = CreateEngine();
		var sim = engine.CreateSimulation();

		Assert.True(sim.TryEnqueue(new AcceptContractAction(unitId, contractId)));
		Assert.Single(sim.Actions);
	}

	[Fact]
	public void TryEnqueue_FailsForUnknownContract()
	{
		var (engine, unitId, _) = CreateEngine();
		var sim = engine.CreateSimulation();

		Assert.False(sim.TryEnqueue(new AcceptContractAction(unitId, "contract-missing")));
		Assert.Empty(sim.Actions);
	}

	[Fact]
	public void TryEnqueue_FailsForAlreadyAcceptedContract()
	{
		var (engine, unitId, contractId) = CreateEngine();
		engine.Commit(new AcceptContractAction(unitId, contractId));
		var sim = engine.CreateSimulation();

		Assert.False(sim.TryEnqueue(new AcceptContractAction(unitId, contractId)));
		Assert.Empty(sim.Actions);
	}

	[Fact]
	public void TryEnqueue_FailsForMissingActor()
	{
		var (engine, _, contractId) = CreateEngine();
		var sim = engine.CreateSimulation();

		Assert.False(sim.TryEnqueue(new AcceptContractAction("missing-actor", contractId)));
		Assert.Empty(sim.Actions);
	}

	[Fact]
	public void TryEnqueue_FailsForRejectedContract()
	{
		var (engine, unitId, contractId) = CreateEngine();
		engine.World.ContractRegistry.Activate(new ContractState(
			contractId,
			EContractStatus.Rejected,
			null,
			null,
			ContractState.EmptyBindings));
		var sim = engine.CreateSimulation();

		Assert.False(sim.TryEnqueue(new AcceptContractAction(unitId, contractId)));
		Assert.Empty(sim.Actions);
	}

	[Fact]
	public void Commit_CreatesActiveContractStateAndRecordsTimeline()
	{
		var (engine, unitId, contractId) = CreateEngine();
		var tick = engine.Tick;
		var initialUnitCount = engine.World.UnitRegistry.All.Count();

		engine.Commit(new AcceptContractAction(unitId, contractId));

		Assert.False(engine.World.ContractRegistry.IsOffered(contractId));
		Assert.True(engine.World.ContractRegistry.TryGetState(contractId, out var state));
		Assert.Equal(EContractStatus.Active, state.Status);
		Assert.Equal(unitId, state.HolderUnitId);
		Assert.Equal(tick, state.AcceptedAtTick);
		Assert.Equal(initialUnitCount + 1, engine.World.UnitRegistry.All.Count());

		Assert.Contains(
			engine.History().OfType<AcceptContractAction>(),
			action => action.ContractId == contractId && action.ActorId == unitId);
	}

	[Fact]
	public void TryCommitPlayerInput_AfterAcceptContract_PlayerCanQueueMove()
	{
		var orchestrator = StarSystemTestHarness.CreatePlayerOrchestrator(
			maps,
			GrimSpace.Run.State.PlayerFleetUnitId,
			42);
		var contractId = orchestrator.Map.ContractRegistry.Offered.First().Id;
		var action = new AcceptContractAction(GrimSpace.Run.State.PlayerFleetUnitId, contractId);

		Assert.True(orchestrator.TryCommitPlayerInput(action));
		Assert.False(orchestrator.Map.ContractRegistry.IsOffered(contractId));
		Assert.True(orchestrator.PlayerAgent!.IsPlanning);

		var destination = new Coord(50, 0, 50);
		Assert.IsType<CourseCommandResult.Queued>(orchestrator.PlayerAgent.TryQueueMove(destination));
	}

	[Fact]
	public void RefreshPlayerAgent_AfterStuckCommitState_RestoresPlanning()
	{
		var orchestrator = StarSystemTestHarness.CreatePlayerOrchestrator(
			maps,
			GrimSpace.Run.State.PlayerFleetUnitId,
			42);
		var agent = orchestrator.PlayerAgent!;
		var contractId = orchestrator.Map.ContractRegistry.Offered.First().Id;

		Assert.True(agent.TryEnqueue([new AcceptContractAction(GrimSpace.Run.State.PlayerFleetUnitId, contractId)]));
		Assert.True(agent.Commit());
		Assert.False(agent.IsPlanning);

		orchestrator.RefreshPlayerAgent();

		Assert.True(agent.IsPlanning);
		Assert.Null(agent.PendingMove);
	}

	private (Engine<StarMap, ActorRuntime> engine, string unitId, string contractId) CreateEngine(
		int seed = 42)
	{
		var map = maps.Fresh(seed);
		var unit = map.UnitRegistry.All.First();
		var unitId = unit.State.Id;

		var runtimes = new ActorRuntimes<ActorRuntime>();
		runtimes.For(unitId);
		var engine = new Engine<StarMap, ActorRuntime>(map, runtimes);
		var contractId = map.ContractRegistry.Offered.First().Id;
		return (engine, unitId, contractId);
	}
}
