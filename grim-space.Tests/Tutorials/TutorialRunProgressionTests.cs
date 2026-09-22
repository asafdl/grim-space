using GrimSpace.Education;
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

public sealed class TutorialRunProgressionTests(StarMapFixture maps)
{
	[Fact]
	public void CreateNewRun_WithTutorialsEnabled_CreatesBeatA()
	{
		using var run = State.CreateNewRun(42, tutorialsEnabled: true);

		Assert.NotNull(run.Tutorials);
		Assert.Equal(TutorialBeat.FirstContract, run.TutorialState!.CurrentBeat);
		Assert.NotNull(run.TutorialState!.BeatAContractId);
		Assert.Single(
			run.StarSystem.Map.ContractRegistry.Offered,
			contract => contract.IsStoryObjective && contract.Objective is HuntObjective);
	}

	[Fact]
	public void CreateNewRun_WithTutorialsDisabled_CreatesNoTutorialDomain()
	{
		using var run = State.CreateNewRun(42, tutorialsEnabled: false);

		Assert.Null(run.Tutorials);
		Assert.Null(run.TutorialProgress);
		Assert.Empty(run.StarSystem.Map.ContractRegistry.Offered);
	}

	[Fact]
	public void Reconcile_AfterBeatACompleted_UsesExplicitBeatAContractId()
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
			new TutorialProgress(),
			new TutorialState { BeatAContractId = beatAId, CurrentBeat = TutorialBeat.FirstContract },
			new TestTutorialRunContext());
		controller.ReconcileBeatTransitions();

		Assert.True(controller.State.BeatBOffered);
		Assert.Equal(TutorialBeat.BeatBDelivery, controller.State.CurrentBeat);
		Assert.NotNull(controller.State.BeatBContractId);
		Assert.True(orchestrator.Map.ContractRegistry.IsOffered(controller.State.BeatBContractId));
		Assert.Contains(
			orchestrator.Map.StoryObjectives.Active,
			objective => objective.RequiredContractId == controller.State.BeatBContractId);
	}

	[Fact]
	public void BeatBObjective_StartsAfterBeatAReturnAndCompletesWhenBeatBIsAccepted()
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
		var progress = new TutorialProgress();
		using var controller = new TutorialController(
			orchestrator,
			progress,
			new TutorialState { BeatAContractId = beatAId, CurrentBeat = TutorialBeat.FirstContract },
			new TestTutorialRunContext());
		controller.AttachMapSubscriptions();
		controller.ReconcileBeatTransitions();
		var beatBId = Assert.IsType<string>(controller.State.BeatBContractId);

		controller.SyncMapFlows();

		Assert.False(controller.IsActive);
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
		Assert.False(controller.IsActive);
	}

	[Fact]
	public void SyncMapFlows_DoesNotStartDialogWhenFirstContractStoryObjectiveIsActive()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, State.PlayerFleetUnitId);
		using var orchestrator = StarSystemOrchestrator.FromMap(map, State.PlayerFleetUnitId);
		orchestrator.Map.StoryObjectives.Add(StoryObjective.FirstContract(
			map.Blueprint.SupplyPlan.AdministrativePoiId));
		using var controller = new TutorialController(
			orchestrator,
			new TutorialProgress(),
			new TutorialState(),
			new TestTutorialRunContext());
		var dialog = new TestDialog();
		using var worldLinks = new WorldLinkNavigator(new TestWorldFocus(), new TestWorldIndicator());
		using var binding = new TutorialPresentationBinding(controller, dialog, worldLinks);
		binding.Attach();
		controller.SyncMapFlows();

		Assert.False(controller.IsActive);
		Assert.Null(dialog.Content);
	}

	[Fact]
	public void ReconcileFromWorldState_CompletesStaleFirstBattleFlowWhenBeatAIsDone()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, State.PlayerFleetUnitId);
		using var orchestrator = StarSystemOrchestrator.FromMap(map, State.PlayerFleetUnitId);
		var beatAId = TutorialBeatContracts.OfferBeatA(orchestrator.Map)!;
		var progress = new TutorialProgress();
		var state = new TutorialState { BeatAContractId = beatAId, CurrentBeat = TutorialBeat.FirstContract };
		using var controller = new TutorialController(
			orchestrator,
			progress,
			state,
			new TestTutorialRunContext());
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
		Assert.True(progress.IsCompleted(FirstBattleTutorial.Id));

		controller.SyncMapFlows(cancelBattleFlowWhenOffBattlefield: true);
		Assert.False(controller.IsActive);
	}
}
