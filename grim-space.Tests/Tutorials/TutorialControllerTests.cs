using GrimSpace.Application;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Education;
using GrimSpace.Math.Grid;
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

	[Fact]
	public void BattleController_StartsAndAdvancesFirstBattleFlow()
	{
		var progress = new TutorialProgress();
		var dialog = new TestDialog();
		var focus = new AcceptingWorldFocus();
		var indicator = new AcceptingWorldIndicator();
		var flow = FirstBattleTutorial.Create();
		using var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		using var controller = TutorialController.CreateForBattle(
			battle.PlayerAgent,
			progress,
			dialog,
			focus,
			indicator);
		TutorialFlow? completedFlow = null;
		var completedQueueCount = -1;
		controller.Completed += flow =>
		{
			completedFlow = flow;
			completedQueueCount = battle.PlayerAgent.Sim.Actions.Count;
		};
		var startedSteps = new List<TutorialStep>();
		controller.StepStarted += (_, step) => startedSteps.Add(step);

		Assert.True(controller.IsActive);
		Assert.Equal(flow.Steps[0].Dialog.Title, dialog.Content?.Title);
		Assert.Equal([FirstBattleTutorial.OverviewTargetId], focus.ObjectIds);
		Assert.Empty(indicator.ObjectIds);
		Assert.False(focus.Handle.IsDisposed);

		dialog.Accept();

		Assert.Equal(flow.Steps[1].Dialog.Title, dialog.Content?.Title);
		Assert.Equal([FirstBattleTutorial.OverviewTargetId], focus.ObjectIds);
		Assert.Equal([FirstBattleTutorial.EnemiesTargetId], indicator.ObjectIds);
		Assert.False(focus.Handle.IsDisposed);

		dialog.Accept();

		Assert.Equal(flow.Steps[2].Dialog.Title, dialog.Content?.Title);
		Assert.Empty(indicator.ObjectIds.Skip(1));
		Assert.False(focus.Handle.IsDisposed);

		dialog.Accept();

		Assert.Equal(flow.Steps[3].Dialog.Title, dialog.Content?.Title);
		Assert.Equal(
			[FirstBattleTutorial.EnemiesTargetId, FirstBattleTutorial.PlayerTargetId],
			indicator.ObjectIds);
		Assert.False(focus.Handle.IsDisposed);
		Assert.Equal(flow.Steps[3], startedSteps[^1]);

		dialog.Accept();

		Assert.Equal(flow.Steps[4].Dialog.Title, dialog.Content?.Title);
		Assert.Equal(
			[FirstBattleTutorial.OverviewTargetId, FirstBattleTutorial.PlayerTargetId],
			focus.ObjectIds);
		Assert.All(focus.Handles, handle => Assert.False(handle.IsDisposed));

		dialog.Accept();

		Assert.Equal(flow.Steps[5].Dialog.Title, dialog.Content?.Title);
		Assert.Null(dialog.Content?.AcceptText);

		dialog.Accept();

		Assert.Equal(flow.Steps[5].Dialog.Title, dialog.Content?.Title);
		for (var attempt = 0; attempt < 2; attempt++)
		{
			Assert.True(battle.PlayerAgent.TryEnqueue(
			[
				new MoveStepAction(battle.PlayerId),
			]));
			Assert.Equal(flow.Steps[5].Dialog.Title, dialog.Content?.Title);
			Assert.Contains("no roll", dialog.Assistance?.Message);
			Assert.Equal("Undo and try again", dialog.Assistance?.ActionText);

			dialog.RequestAssistance();

			Assert.Empty(battle.PlayerAgent.Sim.Actions);
			Assert.Null(dialog.Assistance);
			Assert.Equal(flow.Steps[5].Dialog.Title, dialog.Content?.Title);
		}

		Assert.True(battle.PlayerAgent.TryEnqueue(
		[
			new RollAction(battle.PlayerId, ERollDirection.Clockwise),
			new MoveStepAction(battle.PlayerId),
		]));

		Assert.Equal(flow.Steps[6].Dialog.Title, dialog.Content?.Title);
		Assert.Null(dialog.Content?.AcceptText);

		dialog.Accept();

		Assert.True(controller.IsActive);
		Assert.True(battle.PlayerAgent.Undo());
		controller.NotifyBattleUndoShortcut();

		Assert.True(controller.IsActive);
		Assert.Equal(flow.Steps[7].Dialog.Title, dialog.Content?.Title);
		Assert.Equal(
			[
				FirstBattleTutorial.OverviewTargetId,
				FirstBattleTutorial.PlayerTargetId,
				FirstBattleTutorial.WeaponTrainingTargetId,
			],
			focus.ObjectIds);

		dialog.Accept();

		Assert.Equal(flow.Steps[8].Dialog.Title, dialog.Content?.Title);
		Assert.Null(dialog.Content?.AcceptText);
		Assert.True(battle.PlayerAgent.TryEnqueue(
		[
			new RailgunAction(battle.PlayerId),
		]));
		Assert.Equal(flow.Steps[8].Dialog.Title, dialog.Content?.Title);
		Assert.True(battle.PlayerAgent.Undo());
		Assert.Equal(flow.Steps[8].Dialog.Title, dialog.Content?.Title);

		Assert.True(battle.PlayerAgent.TryEnqueue(
		[
			new FlakAction(battle.PlayerId, ESpatialOrientation.Port),
		]));

		Assert.Equal(flow.Steps[9].Dialog.Title, dialog.Content?.Title);
		Assert.Null(dialog.Content?.AcceptText);
		Assert.Contains(
			BattleTestCommands.Frame(battle).AreaActions.Queued,
			preview => preview.Action is FlakAction);
		Assert.True(battle.PlayerAgent.Undo());

		Assert.True(controller.IsActive);
		Assert.Equal(flow.Steps[10].Dialog.Title, dialog.Content?.Title);
		Assert.Equal("Continue", dialog.Content?.AcceptText);

		dialog.Accept();

		Assert.True(controller.IsActive);
		Assert.Equal(flow.Steps[11].Dialog.Title, dialog.Content?.Title);
		Assert.Equal("Begin Battle", dialog.Content?.AcceptText);
		Assert.True(battle.PlayerAgent.TryEnqueue(
		[
			new RailgunAction(battle.PlayerId),
		]));
		Assert.NotEmpty(battle.PlayerAgent.Sim.Actions);

		dialog.Accept();

		Assert.False(controller.IsActive);
		Assert.False(dialog.IsOpen);
		Assert.True(progress.IsCompleted(FirstBattleTutorial.Id));
		Assert.Equal(FirstBattleTutorial.Id, completedFlow?.Id);
		Assert.False(GameSettings.ReadShowTutorials());
		Assert.Equal(0, completedQueueCount);
		Assert.Empty(battle.PlayerAgent.Sim.Actions);
		Assert.All(focus.Handles, handle => Assert.True(handle.IsDisposed));
	}

	private StarSystemOrchestrator CreateOrchestrator() =>
		StarSystemTestHarness.CreatePlayerOrchestrator(
			maps,
			RunState.PlayerFleetUnitId,
			42);

	private sealed class TestDialog : ITutorialDialog
	{
		public event Action? Accepted;

		public event Action? AssistanceRequested;

		public event Action<string>? WorldLinkClicked;

		public bool IsOpen { get; private set; }

		public TutorialDialogContent? Content { get; private set; }

		public TutorialAssistanceContent? Assistance { get; private set; }

		public void Open(TutorialDialogContent content)
		{
			Content = content;
			Assistance = null;
			IsOpen = true;
		}

		public void ShowAssistance(TutorialAssistanceContent content) => Assistance = content;

		public void ClearAssistance() => Assistance = null;

		public void Close()
		{
			Assistance = null;
			IsOpen = false;
		}

		public void Accept() => Accepted?.Invoke();

		public void RequestAssistance() => AssistanceRequested?.Invoke();

		public void ClickWorldLink(string objectId) => WorldLinkClicked?.Invoke(objectId);
	}

	private sealed class AcceptingWorldFocus : IWorldFocus
	{
		public List<string> ObjectIds { get; } = [];

		public List<FocusHandle> Handles { get; } = [];

		public FocusHandle Handle => Handles[^1];

		public string? ObjectId { get; private set; }

		public WorldFocusResult Focus(string objectId)
		{
			ObjectId = objectId;
			ObjectIds.Add(objectId);
			var handle = new FocusHandle();
			Handles.Add(handle);
			return new WorldFocusResult.Accepted(handle);
		}
	}

	private sealed class FocusHandle : IWorldFocusHandle
	{
		public bool IsDisposed { get; private set; }

		public void Dispose() => IsDisposed = true;
	}

	private sealed class AcceptingWorldIndicator : IWorldIndicator
	{
		public List<string> ObjectIds { get; } = [];

		public string? ObjectId { get; private set; }

		public WorldIndicatorResult Show(string objectId)
		{
			ObjectId = objectId;
			ObjectIds.Add(objectId);
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
