using GrimSpace.Education;
using GrimSpace.Run;
using GrimSpace.Tutorials;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
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
			new AcceptingWorldIndicator(),
			() => true);

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
			new AcceptingWorldIndicator(),
			() => true);

		controller.Sync();
		dialog.Accept();

		Assert.Equal(ESimMode.Stepped, orchestrator.SimMode);
	}

	[Fact]
	public void Sync_AfterFirstContractAccepted_DelaysPirateTutorialUntilMapIsStrategic()
	{
		var orchestrator = CreateOrchestrator();
		var contractId = orchestrator.Map.ContractRegistry.Offered.First().Id;
		Assert.True(orchestrator.TryCommitPlayerInput(
			new AcceptContractAction(RunState.PlayerFleetUnitId, contractId)));
		var pirateId = orchestrator.Map.ContractRegistry
			.ActiveFor(RunState.PlayerFleetUnitId)
			.Single()
			.State.SpawnBindings.Values
			.SelectMany(unitIds => unitIds)
			.Single();
		var progress = new TutorialProgress();
		progress.Complete(FirstContractTutorial.Id);
		var dialog = new TestDialog();
		var focus = new AcceptingWorldFocus();
		var indicator = new AcceptingWorldIndicator();
		var isStrategic = false;
		using var controller = new StarSystemTutorialController(
			orchestrator,
			progress,
			dialog,
			focus,
			indicator,
			() => isStrategic);

		controller.Sync();
		Assert.False(controller.IsActive);
		Assert.False(dialog.IsOpen);

		isStrategic = true;
		controller.Sync();

		Assert.True(controller.IsActive);
		Assert.True(dialog.IsOpen);
		Assert.Equal(pirateId, focus.ObjectId);
		Assert.Equal(pirateId, indicator.ObjectId);
		Assert.Contains($"[url={pirateId}]pirate ship[/url]", dialog.Content!.Message);

		dialog.Accept();

		Assert.True(progress.IsCompleted(FirstPirateTutorial.Id));
	}

	private StarSystemOrchestrator CreateOrchestrator() =>
		StarSystemTestHarness.CreatePlayerOrchestrator(
			maps,
			RunState.PlayerFleetUnitId,
			42);

	private sealed class TestDialog : ITutorialDialog
	{
		public event Action? Accepted;

		public event Action<string>? WorldLinkClicked;

		public bool IsOpen { get; private set; }

		public TutorialDialogContent? Content { get; private set; }

		public void Open(TutorialDialogContent content)
		{
			Content = content;
			IsOpen = true;
		}

		public void Close() => IsOpen = false;

		public void Accept() => Accepted?.Invoke();

		public void ClickWorldLink(string objectId) => WorldLinkClicked?.Invoke(objectId);
	}

	private sealed class AcceptingWorldFocus : IWorldFocus
	{
		public string? ObjectId { get; private set; }

		public WorldFocusResult Focus(string objectId)
		{
			ObjectId = objectId;
			return new WorldFocusResult.Accepted();
		}
	}

	private sealed class AcceptingWorldIndicator : IWorldIndicator
	{
		public string? ObjectId { get; private set; }

		public WorldIndicatorResult Show(string objectId)
		{
			ObjectId = objectId;
			return new WorldIndicatorResult.Shown(new IndicatorHandle());
		}
	}

	private sealed class IndicatorHandle : IWorldIndicatorHandle
	{
		public void Dispose()
		{
		}
	}
}
