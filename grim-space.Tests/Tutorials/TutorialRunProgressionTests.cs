using GrimSpace.Run;
using GrimSpace.Tutorials;
using GrimSpace.Tests.World.StarSystem;
using GrimSpace.Tests.World.StarSystem.Traffic;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Objectives;

namespace GrimSpace.Tests.Tutorials;

[IntegrationTestSuite]
public sealed class TutorialRunProgressionTests(StarMapFixture maps)
{
	[Fact]
	public void CreateNewRun_WithTutorialsEnabled_CreatesBeatA()
	{
		using var run = State.CreateNewRun(42, tutorialsEnabled: true);

		Assert.NotNull(run.Tutorials);
		Assert.NotNull(run.TutorialState!.BeatAContractId);
		Assert.Single(
			run.StarSystem.Map.ContractRegistry.Pending,
			contract => contract.IsStoryObjective && contract.Objective is HuntObjective);
	}

	[Fact]
	public void CreateNewRun_WithTutorialsDisabled_CreatesNoTutorialDomain()
	{
		using var run = State.CreateNewRun(42, tutorialsEnabled: false);

		Assert.Null(run.Tutorials);
		Assert.Null(run.TutorialState);
		Assert.Empty(run.StarSystem.Map.ContractRegistry.Pending);
	}

	[Fact]
	public void InitializeBeatProgression_IsIdempotent()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, State.PlayerFleetUnitId);
		using var orchestrator = StarSystemOrchestrator.FromMap(map, State.PlayerFleetUnitId);
		using var controller = new TutorialController(orchestrator, new TutorialState());

		controller.InitializeBeatProgression();
		controller.InitializeBeatProgression();

		Assert.Single(orchestrator.Map.ContractRegistry.Pending);
	}

	[Fact]
	public void Reconcile_AfterBeatACompleted_OffersBeatB()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, State.PlayerFleetUnitId);
		using var orchestrator = StarSystemOrchestrator.FromMap(map, State.PlayerFleetUnitId);
		var beatAId = TutorialBeatContracts.OfferBeatA(orchestrator.Map)!;
		orchestrator.Map.ContractRegistry.Activate(new ContractState(
			beatAId,
			EContractStatus.Completed,
			1,
			State.PlayerFleetUnitId,
			ContractState.EmptyBindings));

		using var controller = new TutorialController(
			orchestrator,
			new TutorialState { BeatAContractId = beatAId });
		controller.ReconcileBeatTransitions();

		Assert.NotNull(controller.State.BeatBContractId);
		Assert.True(orchestrator.Map.ContractRegistry.IsPending(controller.State.BeatBContractId));
		Assert.Contains(
			orchestrator.Map.StoryObjectives.Active,
			objective => objective.RequiredContractId == controller.State.BeatBContractId);
	}

	[Fact]
	public void BeatBObjective_CompletesWhenBeatBIsAccepted()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, State.PlayerFleetUnitId);
		using var orchestrator = StarSystemOrchestrator.FromMap(map, State.PlayerFleetUnitId);
		var beatAId = TutorialBeatContracts.OfferBeatA(orchestrator.Map)!;
		orchestrator.Map.ContractRegistry.Activate(new ContractState(
			beatAId,
			EContractStatus.Completed,
			1,
			State.PlayerFleetUnitId,
			ContractState.EmptyBindings));
		using var controller = new TutorialController(
			orchestrator,
			new TutorialState { BeatAContractId = beatAId });
		controller.EnsureContractObservation();
		controller.ReconcileBeatTransitions();
		var beatBId = Assert.IsType<string>(controller.State.BeatBContractId);

		Assert.Contains(
			orchestrator.Map.StoryObjectives.Active,
			objective => objective.RequiredContractId == beatBId);
		orchestrator.CommitSetup(new AcceptContractAction(
			State.PlayerFleetUnitId,
			map.Blueprint.SupplyPlan.StoragePoiId,
			"warehouse",
			"manager",
			beatBId));

		Assert.DoesNotContain(
			orchestrator.Map.StoryObjectives.Active,
			objective => objective.RequiredContractId == beatBId);
	}

	[Fact]
	public void OfferBeatB_AfterHuntCompletion_AddsDeliveryAtStorage()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, State.PlayerFleetUnitId);
		using var orchestrator = StarSystemOrchestrator.FromMap(map, State.PlayerFleetUnitId);
		using var controller = new TutorialController(orchestrator, new TutorialState());
		controller.InitializeBeatProgression();
		var huntId = orchestrator.Map.ContractRegistry.Pending.Single().Id;
		orchestrator.Map.ContractRegistry.Activate(new ContractState(
			huntId,
			EContractStatus.Completed,
			1,
			State.PlayerFleetUnitId,
			ContractState.EmptyBindings));

		TutorialBeatContracts.OfferBeatB(orchestrator.Map);

		var delivery = orchestrator.Map.ContractRegistry.Pending
			.Single(contract => contract.Objective is DeliveryObjective);
		Assert.Equal(map.Blueprint.SupplyPlan.StoragePoiId, delivery.IssuerPoiId);
		Assert.True(delivery.IsStoryObjective);
		Assert.False(delivery.AllowsDecline);
	}

	[Fact]
	public void ReconcileFromWorldState_CompletesStaleFirstBattleFlowWhenBeatAIsDone()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, State.PlayerFleetUnitId);
		using var orchestrator = StarSystemOrchestrator.FromMap(map, State.PlayerFleetUnitId);
		var beatAId = TutorialBeatContracts.OfferBeatA(orchestrator.Map)!;
		var state = new TutorialState { BeatAContractId = beatAId };
		using var controller = new TutorialController(orchestrator, state);
		controller.TryStartFlow(FirstBattleTutorial.Create());
		Assert.True(controller.IsActive);

		orchestrator.Map.ContractRegistry.Activate(new ContractState(
			beatAId,
			EContractStatus.Completed,
			1,
			State.PlayerFleetUnitId,
			ContractState.EmptyBindings));
		controller.ReconcileFromWorldState(cancelBattleFlowWhenOffBattlefield: true);

		Assert.False(controller.IsActive);
		Assert.True(state.IsFlowCompleted(FirstBattleTutorial.Id));
	}
}
