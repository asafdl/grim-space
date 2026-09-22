using GrimSpace.Education;
using GrimSpace.Run;
using GrimSpace.Tutorials;
using GrimSpace.Tests.World.StarSystem;
using GrimSpace.Tests.World.StarSystem.Traffic;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts;

namespace GrimSpace.Tests.Tutorials;

public sealed class TutorialGraduationTests(StarMapFixture maps)
{
	[Fact]
	public void TutorialGraduation_Flow_HasAcceptStep()
	{
		var flow = TutorialGraduation.Create();
		Assert.Equal(TutorialGraduation.Id, flow.Id);
		var step = Assert.Single(flow.Steps);
		Assert.Null(step.TargetId);
		Assert.True(step.AdvanceOnAccept);
		Assert.Equal("Accept", step.Dialog.AcceptText);
		Assert.Contains("settings", step.Dialog.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void BeatBCompletion_StartsGraduationDialog_AndAcceptCompletesFlow()
	{
		using var orchestrator = StarSystemTestHarness.CreatePlayerOrchestrator(
			maps,
			State.PlayerFleetUnitId,
			42);
		var beatAId = TutorialBeatContracts.OfferBeatA(orchestrator.Map)!;
		orchestrator.Map.ContractRegistry.Activate(new ContractState(
			beatAId,
			EContractStatus.Completed,
			1,
			State.PlayerFleetUnitId,
			ContractState.EmptyBindings));
		var progress = new TutorialProgress();
		var tutorialState = new TutorialState
		{
			BeatAContractId = beatAId,
			CurrentBeat = TutorialBeat.FirstContract,
		};
		var runContext = new TestTutorialRunContext();
		using var controller = new TutorialController(
			orchestrator,
			progress,
			tutorialState,
			runContext);
		controller.AttachMapSubscriptions();
		controller.ReconcileBeatTransitions();
		var beatBId = Assert.IsType<string>(tutorialState.BeatBContractId);
		var dialog = new TestDialog();
		using var worldLinks = new WorldLinkNavigator(new TestWorldFocus(), new TestWorldIndicator());
		using var binding = new TutorialPresentationBinding(controller, dialog, worldLinks);
		binding.Attach();

		controller.NotifyDeliveryContractCompleted(beatBId);
		controller.PresentGraduationIfPending();

		Assert.True(runContext.PendingTutorialGraduation);
		Assert.Equal(TutorialGraduation.Id, controller.ActiveFlow?.Id);
		Assert.NotNull(dialog.Content);
		Assert.Contains("settings", dialog.Content!.Message, StringComparison.OrdinalIgnoreCase);

		dialog.SimulateAccept();

		Assert.True(progress.IsCompleted(TutorialGraduation.Id));
		Assert.False(runContext.PendingTutorialGraduation);
		Assert.False(controller.IsActive);
	}

	[Fact]
	public void NotifyDeliveryContractCompleted_OnlySchedulesGraduationForBeatB()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, State.PlayerFleetUnitId);
		using var orchestrator = StarSystemOrchestrator.FromMap(map, State.PlayerFleetUnitId);
		var runContext = new TestTutorialRunContext();
		using var controller = new TutorialController(
			orchestrator,
			new TutorialProgress(),
			new TutorialState { BeatBContractId = "tutorial-beat-b" },
			runContext);

		controller.NotifyDeliveryContractCompleted("other-delivery");

		Assert.False(runContext.PendingTutorialGraduation);
	}
}
