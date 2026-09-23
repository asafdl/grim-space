using GrimSpace.Education;
using GrimSpace.Run;
using GrimSpace.Tutorials;
using GrimSpace.Tests.World.StarSystem;
using GrimSpace.Tests.World.StarSystem.Traffic;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts;

namespace GrimSpace.Tests.Tutorials;

[IntegrationTestSuite]
public sealed class TutorialGraduationTests(StarMapFixture maps)
{
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
		var tutorialState = new TutorialState { BeatAContractId = beatAId };
		using var controller = new TutorialController(orchestrator, tutorialState);
		controller.EnsureContractObservation();
		controller.ReconcileBeatTransitions();
		var beatBId = Assert.IsType<string>(tutorialState.BeatBContractId);
		var dialog = new TestDialog();
		using var worldLinks = new WorldLinkNavigator(new TestWorldFocus(), new TestWorldIndicator());
		using var binding = new TutorialPresentationBinding(controller, dialog, worldLinks);
		binding.Attach();

		controller.NotifyDeliveryContractCompleted(beatBId);
		controller.PresentGraduationIfPending();

		Assert.True(tutorialState.PendingTutorialGraduation);
		Assert.Equal(TutorialController.GraduationFlowId, controller.ActiveFlow?.Id);
		Assert.NotNull(dialog.Content);
		Assert.Contains("settings", dialog.Content!.Message, StringComparison.OrdinalIgnoreCase);

		dialog.SimulateAccept();

		Assert.True(tutorialState.IsFlowCompleted(TutorialController.GraduationFlowId));
		Assert.False(tutorialState.PendingTutorialGraduation);
		Assert.False(controller.IsActive);
	}
}
