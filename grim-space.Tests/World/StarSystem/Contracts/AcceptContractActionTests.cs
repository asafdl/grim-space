using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

public sealed class AcceptContractActionTests
{
	[Fact]
	public void TryEnqueue_SucceedsWhenActorDockedAtIssuer()
	{
		var (engine, unitId, contractId) = CreateEngineAtIssuerDock();
		var sim = engine.CreateSimulation();

		Assert.True(sim.TryEnqueue(new AcceptContractAction(unitId, contractId)));
		Assert.Single(sim.Actions);
	}

	[Fact]
	public void TryEnqueue_FailsForUnknownContract()
	{
		var (engine, unitId, _) = CreateEngineAtIssuerDock();
		var sim = engine.CreateSimulation();

		Assert.False(sim.TryEnqueue(new AcceptContractAction(unitId, "contract-missing")));
		Assert.Empty(sim.Actions);
	}

	[Fact]
	public void TryEnqueue_FailsForAlreadyAcceptedContract()
	{
		var (engine, unitId, contractId) = CreateEngineAtIssuerDock();
		engine.Commit(new AcceptContractAction(unitId, contractId));
		var sim = engine.CreateSimulation();

		Assert.False(sim.TryEnqueue(new AcceptContractAction(unitId, contractId)));
		Assert.Empty(sim.Actions);
	}

	[Fact]
	public void TryEnqueue_FailsForMissingActor()
	{
		var (engine, _, contractId) = CreateEngineAtIssuerDock();
		var sim = engine.CreateSimulation();

		Assert.False(sim.TryEnqueue(new AcceptContractAction("missing-actor", contractId)));
		Assert.Empty(sim.Actions);
	}

	[Fact]
	public void TryEnqueue_FailsWhenNotDockedAtIssuer()
	{
		var (engine, unitId, contractId) = CreateEngineAtIssuerDock();
		var map = engine.World;
		var plan = map.Blueprint.SupplyPlan;
		map.UnitRegistry.UnitOf(unitId).State.DockedAtDockId =
			map.DocksByPoiId[plan.StoragePoiId].Id;

		var sim = engine.CreateSimulation();
		Assert.False(sim.TryEnqueue(new AcceptContractAction(unitId, contractId)));
		Assert.Empty(sim.Actions);
	}

	[Fact]
	public void Commit_CreatesActiveContractStateAndRecordsTimeline()
	{
		var (engine, unitId, contractId) = CreateEngineAtIssuerDock();
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

	private static (Engine<StarMap, ActorRuntime> engine, string unitId, string contractId) CreateEngineAtIssuerDock(
		int seed = 42)
	{
		var map = StarMap.CreateDevDefault(seed);
		var unit = map.UnitRegistry.All.First();
		var unitId = unit.State.Id;
		var issuerDockId = map.DocksByPoiId[map.Blueprint.SupplyPlan.AdministrativePoiId].Id;
		unit.State.Phase = EPhase.Docked;
		unit.State.DockedAtDockId = issuerDockId;

		var runtimes = new ActorRuntimes<ActorRuntime>();
		runtimes.Register(unitId, unit.Runtime);
		var engine = new Engine<StarMap, ActorRuntime>(map, runtimes);
		var contractId = map.ContractRegistry.Offered.First().Id;
		return (engine, unitId, contractId);
	}
}
