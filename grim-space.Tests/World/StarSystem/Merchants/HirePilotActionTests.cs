using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Run.Items;
using GrimSpace.Units;
using GrimSpace.Units.Specs;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.Tests.World.StarSystem.Contracts;
using GrimSpace.Tests.World.StarSystem.Traffic;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

[StarSystemTestSuite]
public sealed class HirePilotActionTests
{
	[Fact]
	public void Commit_MarksSlotPurchasedAndEmitsPilotHireCommitted()
	{
		var (engine, contract) = CreateAcceptedPilotsLeadEngine("lead-hire-success");
		var map = engine.World;
		Assert.True(PilotRecruitment.TryGetHireCandidate(map, contract.Id, 1, out var candidate));
		SeedResources(map, PilotRecruitment.Price(map, candidate));
		var action = new HirePilotAction(
			State.PlayerFleetUnitId,
			new LeadSaleMerchantEndpoint(contract.Id),
			1);

		var history = engine.Commit(action);

		var lead = GetLeadState(map, contract.Id);
		Assert.Contains(1, lead.PurchasedPilotHireSlots);
		var committed = Assert.IsType<Record<PilotHireCommitted>>(
			Assert.Single(history, entry => entry is Record<PilotHireCommitted>));
		Assert.Equal(candidate.Profile.Name, committed.Value.Profile.Name);
		Assert.Equal(candidate.Profile.BackgroundId, committed.Value.Profile.BackgroundId);
	}

	[Fact]
	public void Simulation_Dequeue_UndoesDebitAndPurchasedSlot()
	{
		var (engine, contract) = CreateAcceptedPilotsLeadEngine("lead-hire-undo");
		var map = engine.World;
		Assert.True(PilotRecruitment.TryGetHireCandidate(map, contract.Id, 0, out var candidate));
		var price = PilotRecruitment.Price(map, candidate);
		SeedResources(map, price);
		var creditsBefore = map.PlayerResources.GetBalance(ResourceId.Credits);
		var action = new HirePilotAction(
			State.PlayerFleetUnitId,
			new LeadSaleMerchantEndpoint(contract.Id),
			0);
		var simulation = engine.CreateSimulation();
		Assert.True(simulation.TryEnqueue(action));
		Assert.Contains(0, GetLeadState(simulation.World, contract.Id).PurchasedPilotHireSlots);
		price.TryGet(ResourceId.Credits, out var creditCost);
		Assert.Equal(creditsBefore - creditCost, simulation.World.PlayerResources.GetBalance(ResourceId.Credits));

		simulation.Dequeue();
		Assert.Empty(GetLeadState(simulation.World, contract.Id).PurchasedPilotHireSlots);
		Assert.Equal(creditsBefore, simulation.World.PlayerResources.GetBalance(ResourceId.Credits));
	}

	[Fact]
	public void IsLegal_RejectsPurchasedSlot()
	{
		var (engine, contract) = CreateAcceptedPilotsLeadEngine("lead-hire-double");
		var map = engine.World;
		Assert.True(PilotRecruitment.TryGetHireCandidate(map, contract.Id, 0, out var candidate));
		SeedResources(map, PilotRecruitment.Price(map, candidate));
		var endpoint = new LeadSaleMerchantEndpoint(contract.Id);
		var action = new HirePilotAction(State.PlayerFleetUnitId, endpoint, 0);
		var runtime = engine.ActorRuntimes.For(State.PlayerFleetUnitId);
		Assert.True(engine.CreateSimulation().TryEnqueue(action));
		engine.Commit(action);

		Assert.False(HirePilotActionDef.Instance.IsLegal(action, map, runtime));
	}

	[Theory]
	[InlineData(9, true)]
	[InlineData(10, false)]
	public void Commit_RespectsTenPilotRosterLimit(int pilotCount, bool canHire)
	{
		var (engine, contract) = CreateAcceptedPilotsLeadEngine("lead-hire-large-roster");
		var map = engine.World;
		map.PilotRegistry = new RunPilotRegistry();
		for (var index = 0; index < pilotCount; index++)
			map.PilotRegistry.Register(Pilot.CreateForShip($"existing-{index}"));
		Assert.True(PilotRecruitment.TryGetHireCandidate(map, contract.Id, 0, out var candidate));
		var price = PilotRecruitment.Price(map, candidate);
		SeedResources(map, price);
		var action = new HirePilotAction(
			State.PlayerFleetUnitId,
			new LeadSaleMerchantEndpoint(contract.Id),
			0);
		var creditsBefore = map.PlayerResources.GetBalance(ResourceId.Credits);

		Assert.Equal(canHire, engine.CreateSimulation().TryEnqueue(action));
		if (!canHire)
		{
			Assert.Empty(GetLeadState(map, contract.Id).PurchasedPilotHireSlots);
			Assert.Equal(creditsBefore, map.PlayerResources.GetBalance(ResourceId.Credits));
			return;
		}
		var history = engine.Commit(action);

		Assert.Contains(0, GetLeadState(map, contract.Id).PurchasedPilotHireSlots);
		Assert.Single(history, entry => entry is Record<PilotHireCommitted>);
		Assert.True(price.TryGet(ResourceId.Credits, out var creditCost));
		Assert.Equal(creditsBefore - creditCost, map.PlayerResources.GetBalance(ResourceId.Credits));
	}

	private static (Engine<StarMap, ActorRuntime> Engine, Contract Contract) CreateAcceptedPilotsLeadEngine(
		string contractId)
	{
		var registry = new Inventory();
		var map = StarMap.Create(42, registry);
		StarSystemTestHarness.AddPlayerFleet(map, State.PlayerFleetUnitId);
		var contract = ContractFactory.Build(
			map,
			contractId,
			EContractKind.Lead,
			new LeadCreateArgs(
				ELeadKind.BlackMarketSale,
				new ContractNarrative("Quiet Opportunity", "A seller is available."),
				300,
				SpaceMeetingChance: 0,
				MerchantSubtype: EMerchantSubtype.Pilots));
		Assert.True(map.ContractRegistry.TryAdd(contract));
		var shipId = map.FleetRegistry.FleetOf(State.PlayerFleetUnitId).Members[0].Id;
		registry.Register(
			ShipInstance.FromSpec(shipId, FighterSpec.Instance, ShipCatalog.FullFighterLoadout()),
			State.PlayerFleetUnitId);
		map = map.Fork();

		var runtimes = new ActorRuntimes<ActorRuntime>();
		runtimes.For(State.PlayerFleetUnitId);
		var engine = new Engine<StarMap, ActorRuntime>(map, runtimes);
		engine.Commit(ContractActionTestContext.Accept(engine.World, State.PlayerFleetUnitId, contract.Id));
		return (engine, contract);
	}

	private static LeadContractState GetLeadState(StarMap map, string contractId)
	{
		Assert.True(map.ContractRegistry.TryGetState(contractId, out var state));
		return Assert.IsType<LeadContractState>(state);
	}

	private static void SeedResources(StarMap map, ResourceBundle bundle)
	{
		var runtime = new ActorRuntime();
		var doubled = new Dictionary<ResourceId, int>();
		foreach (var (id, amount) in bundle)
			doubled[id] = amount * 2;
		new ChangeResourceEffect(TransactionSource.MerchantPurchase, ResourceBundle.Create(doubled))
			.Apply(map, runtime, "seed");
	}
}
