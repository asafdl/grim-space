using GrimSpace.Education;
using GrimSpace.Run;
using GrimSpace.Tutorials;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Objectives;
using GrimSpace.Tests.World.StarSystem;
using GrimSpace.Tests.World.StarSystem.Traffic;
using RunState = GrimSpace.Run.State;

namespace GrimSpace.Tests.Tutorials;

public sealed class StarSystemTutorialControllerTests(DevStarMapFixture maps)
{
	[Fact]
	public void Sync_FirstContractObjectivePausesUntilDialogIsAccepted()
	{
		var orchestrator = CreateOrchestrator();
		orchestrator.Map.StoryObjectives.Add(StoryObjective.FirstContract);
		var progress = new TutorialProgress();
		var dialog = new TestDialog();
		using var controller = new StarSystemTutorialController(
			orchestrator,
			progress,
			dialog,
			new AcceptingWorldFocus(),
			new AcceptingWorldIndicator());

		controller.Sync();

		Assert.True(controller.IsActive);
		Assert.True(dialog.IsOpen);
		Assert.Equal(ESimMode.Stepped, orchestrator.SimMode);
		Assert.False(orchestrator.CanAdvance);

		dialog.Accept();

		Assert.False(controller.IsActive);
		Assert.False(dialog.IsOpen);
		Assert.True(progress.IsCompleted(FirstContractTutorial.Id));
		Assert.Equal(ESimMode.Running, orchestrator.SimMode);
		Assert.True(orchestrator.CanAdvance);
	}

	[Fact]
	public void Accept_DoesNotResumeSimulationPausedBeforeTutorial()
	{
		var orchestrator = CreateOrchestrator();
		orchestrator.SetStepped();
		orchestrator.Map.StoryObjectives.Add(StoryObjective.FirstContract);
		var dialog = new TestDialog();
		using var controller = new StarSystemTutorialController(
			orchestrator,
			new TutorialProgress(),
			dialog,
			new AcceptingWorldFocus(),
			new AcceptingWorldIndicator());

		controller.Sync();
		dialog.Accept();

		Assert.Equal(ESimMode.Stepped, orchestrator.SimMode);
	}

	private StarSystemOrchestrator CreateOrchestrator() =>
		StarSystemTestHarness.CreatePlayerOrchestrator(
			maps,
			RunState.PlayerFleetUnitId,
			42);

	private sealed class TestDialog : ITutorialDialog
	{
		public event Action? Accepted;

		public bool IsOpen { get; private set; }

		public void Open(TutorialDialogContent content) => IsOpen = true;

		public void Close() => IsOpen = false;

		public void Accept() => Accepted?.Invoke();
	}

	private sealed class AcceptingWorldFocus : IWorldFocus
	{
		public WorldFocusResult Focus(string objectId) => new WorldFocusResult.Accepted();
	}

	private sealed class AcceptingWorldIndicator : IWorldIndicator
	{
		public WorldIndicatorResult Show(string objectId) =>
			new WorldIndicatorResult.Shown(new IndicatorHandle());
	}

	private sealed class IndicatorHandle : IWorldIndicatorHandle
	{
		public void Dispose()
		{
		}
	}
}
