using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.Tests.World.StarSystem.Traffic;
using RunState = GrimSpace.Run.State;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class WreckDecisionActionTests(StarMapFixture maps)
{
	[Fact]
	public void ReachWreckage_SetsPendingAndWaitingForPlayer()
	{
		var (engine, unitId, contractId) = CreateEngine(new WreckageOutcome.Salvage(ResourceBundle.Empty));
		PlaceHolderAtWreck(engine, unitId, contractId);

		engine.Commit([new ReachWreckageAction(unitId, contractId)]);

		Assert.Equal(contractId, engine.World.StateOf(unitId).PendingWreckContractId);
		Assert.True(engine.World.WaitingForPlayerInput);
		Assert.False(engine.World.StateOf(unitId).TravelTarget.IsActive);
	}

	[Fact]
	public void ReachWreckage_RejectsDuplicateWhilePending()
	{
		var (engine, unitId, contractId) = CreateEngine(new WreckageOutcome.Salvage(ResourceBundle.Empty));
		PlaceHolderAtWreck(engine, unitId, contractId);
		engine.Commit([new ReachWreckageAction(unitId, contractId)]);

		var sim = engine.CreateSimulation();
		Assert.False(sim.TryEnqueue(new ReachWreckageAction(unitId, contractId)));
	}

	[Fact]
	public void LeaveWreckage_ClearsPendingAndResumesPlayback()
	{
		var (engine, unitId, contractId) = CreateEngine(new WreckageOutcome.Salvage(ResourceBundle.Empty));
		PlaceHolderAtWreck(engine, unitId, contractId);
		engine.Commit([new ReachWreckageAction(unitId, contractId)]);

		engine.Commit([new LeaveWreckageAction(unitId)]);

		Assert.Equal("", engine.World.StateOf(unitId).PendingWreckContractId);
		Assert.False(engine.World.WaitingForPlayerInput);
		Assert.True(engine.World.ContractRegistry.TryGetState(contractId, out var state)
			&& state.Status == EContractStatus.Active);
	}

	[Fact]
	public void PendingWreckDecision_RestoredAfterOrchestratorRecreate()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var contract = RegisterWreckageContract(map, "contract-wreck-restore", new WreckageOutcome.Salvage(ResourceBundle.Empty));
		var orchestrator = StarSystemTestHarness.CreatePlayerOrchestrator(maps, RunState.PlayerFleetUnitId, 42, map: map);
		var engine = new Engine<StarMap, ActorRuntime>(map, new ActorRuntimes<ActorRuntime>());
		PlaceHolderAtWreck(engine, RunState.PlayerFleetUnitId, contract.Id);
		engine.Commit([new ReachWreckageAction(RunState.PlayerFleetUnitId, contract.Id)]);

		var recreated = StarSystemTestHarness.CreatePlayerOrchestrator(maps, RunState.PlayerFleetUnitId, 42, map: map);

		Assert.True(WreckageQueries.TryGetPendingPlayerWreckDecision(
			recreated.Map,
			RunState.PlayerFleetUnitId,
			out var pending));
		Assert.Equal(contract.Id, pending.ContractId);
	}

	private (Engine<StarMap, ActorRuntime> Engine, string UnitId, string ContractId) CreateEngine(
		WreckageOutcome outcome,
		int seed = 42)
	{
		var map = maps.Fresh(seed);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var contract = RegisterWreckageContract(map, "contract-wreck-decision", outcome);

		var runtimes = new ActorRuntimes<ActorRuntime>();
		runtimes.For(RunState.PlayerFleetUnitId);
		var engine = new Engine<StarMap, ActorRuntime>(map, runtimes);
		return (engine, RunState.PlayerFleetUnitId, contract.Id);
	}

	private static void PlaceHolderAtWreck(Engine<StarMap, ActorRuntime> engine, string unitId, string contractId)
	{
		engine.Commit(ContractActionTestContext.Accept(engine.World, unitId, contractId));
		var wreck = (WreckageObjective)engine.World.ContractRegistry.All
			.First(contract => contract.Id == contractId)
			.Objective;
		UpdateLocationEffect.ArriveAtCoord(unitId, wreck.Position)
			.Apply(engine.World, engine.ActorRuntimes.For(unitId), unitId);
	}

	private static Contract RegisterWreckageContract(StarMap map, string contractId, WreckageOutcome outcome)
	{
		var center = new Coord(map.Width / 2, 0, map.Height / 2);
		var searchArea = new AreaPick(
			new AreaIntel(
				"Somewhere near {A}.",
				ContractActionTestContext.AdministrativePoiId,
				ContractActionTestContext.AdministrativePoiId,
				ContractActionTestContext.AdministrativePoiId),
			[center]);
		var contract = new Contract(
			contractId,
			new WreckageObjective($"{contractId}.wreckage", searchArea, outcome),
			GrimSpace.World.StarSystem.Encounter.EDangerLevel.VeryLow,
			map.ControllingFaction,
			ContractActionTestContext.AdministrativePoiId,
			new ContractTerms(ResourceBundle.Of(ResourceId.Credits, 75)),
			new ContractNarrative("Test Wreck", "Investigate the debris."),
			ContractFactory.IsWreckageObjectiveMet);
		Assert.True(map.ContractRegistry.TryAdd(contract));
		return contract;
	}
}
