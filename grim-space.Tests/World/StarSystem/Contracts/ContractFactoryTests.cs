using GrimSpace.Run;
using GrimSpace.Tests.Tutorials;
using GrimSpace.Tutorials;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.Tests.World.StarSystem;
using GrimSpace.Tests.World.StarSystem.Traffic;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

public sealed class ContractFactoryTests(StarMapFixture maps)
{
	[Fact]
	public void Create_Hunt_RegistersOfferedContract()
	{
		var map = maps.Fresh(42);
		var args = TutorialBeatContracts.CreateBeatAHuntArgs(map);

		var contract = ContractFactory.Create(map, EContractKind.Hunt, args);

		Assert.True(map.ContractRegistry.IsOffered(contract.Id));
		Assert.IsType<HuntObjective>(contract.Objective);
		Assert.Equal(args.IssuerPoiId, contract.IssuerPoiId);
		Assert.True(contract.IsStoryObjective);
	}

	[Fact]
	public void Create_Hunt_WithMismatchedArgs_Throws()
	{
		var map = maps.Fresh(42);

		var deliveryArgs = TutorialBeatContracts.CreateBeatBDeliveryArgs(map);
		Assert.Throws<ArgumentException>(() =>
			ContractFactory.Create(map, EContractKind.Hunt, deliveryArgs));
	}
}

public sealed class TutorialBeatProgressionTests(StarMapFixture maps)
{
	[Fact]
	public void Create_EmptyContractRegistry()
	{
		var map = StarMap.Create(42);

		Assert.Empty(map.ContractRegistry.Offered);
	}

	[Fact]
	public void OfferBeatA_AddsStoryHuntAtAdministrativePoi()
	{
		var map = maps.Fresh(42);
		var plan = map.Blueprint.SupplyPlan;

		TutorialBeatContracts.OfferBeatA(map);

		var contract = Assert.Single(map.ContractRegistry.Offered);
		Assert.Equal(plan.AdministrativePoiId, contract.IssuerPoiId);
		Assert.True(contract.IsStoryObjective);
	}

	[Fact]
	public void InitializeBeatProgression_IsIdempotent()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, State.PlayerFleetUnitId);
		using var orchestrator = StarSystemOrchestrator.FromMap(map, State.PlayerFleetUnitId);
		using var controller = CreateController(orchestrator);

		controller.InitializeBeatProgression();
		controller.InitializeBeatProgression();

		Assert.Single(orchestrator.Map.ContractRegistry.Offered);
	}

	[Fact]
	public void Reconcile_AfterBeatACompletedWhileMapWasUnloaded_OffersBeatB()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, State.PlayerFleetUnitId);
		using var orchestrator = StarSystemOrchestrator.FromMap(map, State.PlayerFleetUnitId);
		var beatAId = TutorialBeatContracts.OfferBeatA(orchestrator.Map);
		Assert.NotNull(beatAId);
		orchestrator.Map.ContractRegistry.Activate(new ContractState(
			beatAId!,
			EContractStatus.Completed,
			1,
			State.PlayerFleetUnitId,
			ContractState.EmptyBindings));

		using var controller = CreateController(orchestrator);
		controller.State.BeatAContractId = beatAId;
		controller.State.CurrentBeat = TutorialBeat.FirstContract;
		controller.ReconcileBeatTransitions();

		Assert.Single(
			orchestrator.Map.ContractRegistry.All,
			contract => contract.IsStoryObjective && contract.Objective is HuntObjective);
		Assert.Single(
			orchestrator.Map.ContractRegistry.Offered,
			contract => contract.Objective is DeliveryObjective);
	}

	[Fact]
	public void OfferBeatB_AfterHuntCompletion_AddsDeliveryAtStorage()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, State.PlayerFleetUnitId);
		using var orchestrator = StarSystemOrchestrator.FromMap(map, State.PlayerFleetUnitId);
		using var controller = CreateController(orchestrator);
		controller.InitializeBeatProgression();
		var huntId = orchestrator.Map.ContractRegistry.Offered.Single().Id;
		orchestrator.Map.ContractRegistry.Activate(new ContractState(
			huntId,
			EContractStatus.Completed,
			1,
			State.PlayerFleetUnitId,
			ContractState.EmptyBindings));

		TutorialBeatContracts.OfferBeatB(orchestrator.Map);

		Assert.Equal(2, orchestrator.Map.ContractRegistry.All.Count());
		var delivery = orchestrator.Map.ContractRegistry.Offered
			.Single(contract => contract.Objective is DeliveryObjective);
		Assert.Equal(map.Blueprint.SupplyPlan.StoragePoiId, delivery.IssuerPoiId);
	}

	private static TutorialController CreateController(StarSystemOrchestrator orchestrator) =>
		new(
			orchestrator,
			new TutorialProgress(),
			new TutorialState(),
			new TestTutorialRunContext());
}
