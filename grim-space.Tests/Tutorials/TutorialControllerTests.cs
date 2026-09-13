using GrimSpace.Education;
using GrimSpace.Run;
using GrimSpace.Tutorials;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Narrative;
using GrimSpace.World.StarSystem.Objectives;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.Tests.World.StarSystem;
using GrimSpace.Tests.World.StarSystem.Traffic;
using RunState = GrimSpace.Run.State;

namespace GrimSpace.Tests.Tutorials;

public sealed class TutorialControllerTests(StarMapFixture maps)
{
	[Fact]
	public void Sync_FirstContractObjectiveStartsWithoutPausingSimulation()
	{
		var orchestrator = CreateOrchestrator();
		orchestrator.Map.StoryObjectives.Add(StoryObjective.FirstContract);
		var progress = new TutorialProgress();
		var dialog = new TestDialog();
		using var controller = new TutorialController(
			orchestrator,
			progress,
			dialog,
			new AcceptingWorldFocus(),
			new AcceptingWorldIndicator());

		controller.Sync();

		Assert.True(controller.IsActive);
		Assert.True(dialog.IsOpen);
		Assert.Equal(ESimMode.Running, orchestrator.SimMode);
		Assert.True(orchestrator.CanAdvance);

		dialog.Accept();

		Assert.False(controller.IsActive);
		Assert.False(dialog.IsOpen);
		Assert.True(progress.IsCompleted(FirstContractTutorial.Id));
		Assert.Equal(ESimMode.Running, orchestrator.SimMode);
		Assert.True(orchestrator.CanAdvance);
	}

	[Fact]
	public void CommittedOpeningNarrativeCompletion_StartsFirstContractTutorial()
	{
		var orchestrator = CreateOrchestrator();
		orchestrator.CommitSetup(
			new BeginNarrativeAction(RunState.PlayerFleetUnitId, MapNarratives.OpeningId));
		var dialog = new TestDialog();
		using var controller = new TutorialController(
			orchestrator,
			new TutorialProgress(),
			dialog,
			new AcceptingWorldFocus(),
			new AcceptingWorldIndicator());

		orchestrator.CommitSetup(
			new CompleteNarrativeAction(RunState.PlayerFleetUnitId, MapNarratives.OpeningId));

		Assert.True(controller.IsActive);
		Assert.True(dialog.IsOpen);
		Assert.Equal("Your first contract", dialog.Content?.Title);
	}

	[Fact]
	public void CommittedMoveToRequestedPoi_CompletesFirstContractTutorial()
	{
		var orchestrator = CreateOrchestrator();
		orchestrator.Map.StoryObjectives.Add(StoryObjective.FirstContract);
		var progress = new TutorialProgress();
		var dialog = new TestDialog();
		using var controller = new TutorialController(
			orchestrator,
			progress,
			dialog,
			new AcceptingWorldFocus(),
			new AcceptingWorldIndicator());
		controller.Sync();

		var playerId = RunState.PlayerFleetUnitId;
		var player = orchestrator.Map.FleetRegistry.FleetOf(playerId);
		var origin = orchestrator.Map.DocksById[player.State.DockedAtDockId].Position;
		var administrativeDock = orchestrator.Map.DocksByPoiId[
			orchestrator.Map.Blueprint.SupplyPlan.AdministrativePoiId];
		var path = TransitPath.FromPoints([origin, administrativeDock.Position], [1.0, 1.0]);

		orchestrator.CommitSetup(
			new MoveAction(playerId, playerId, administrativeDock.Position, path));

		Assert.False(controller.IsActive);
		Assert.False(dialog.IsOpen);
		Assert.True(progress.IsCompleted(FirstContractTutorial.Id));
	}

	[Fact]
	public void Sync_DoesNotChangeExistingSimulationMode()
	{
		var orchestrator = CreateOrchestrator();
		orchestrator.SetStepped();
		orchestrator.Map.StoryObjectives.Add(StoryObjective.FirstContract);
		var dialog = new TestDialog();
		using var controller = new TutorialController(
			orchestrator,
			new TutorialProgress(),
			dialog,
			new AcceptingWorldFocus(),
			new AcceptingWorldIndicator());

		controller.Sync();
		dialog.Accept();

		Assert.Equal(ESimMode.Stepped, orchestrator.SimMode);
	}

	[Fact]
	public void CommittedHuntOfRequestedFleet_CompletesFirstPirateTutorial()
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
		using var controller = new TutorialController(
			orchestrator,
			progress,
			dialog,
			new AcceptingWorldFocus(),
			new AcceptingWorldIndicator());
		controller.Sync();

		var origin = orchestrator.CommittedPositionOf(RunState.PlayerFleetUnitId);
		var destination = orchestrator.CommittedPositionOf(pirateId);
		var path = TransitPath.FromPoints([origin, destination], [1.0, 1.0]);
		orchestrator.CommitSetup(
			new HuntUnitAction(RunState.PlayerFleetUnitId, pirateId, destination, path));

		Assert.False(controller.IsActive);
		Assert.False(dialog.IsOpen);
		Assert.True(progress.IsCompleted(FirstPirateTutorial.Id));
	}

	[Fact]
	public void Sync_AfterFirstContractAccepted_StartsPirateTutorial()
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
		using var controller = new TutorialController(
			orchestrator,
			progress,
			dialog,
			focus,
			indicator);

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
