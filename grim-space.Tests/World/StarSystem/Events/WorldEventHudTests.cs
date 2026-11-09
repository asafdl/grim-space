using GrimSpace.Core.Actions;
using GrimSpace.Tests.Run;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Events;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Presentation.Scene;
using GrimSpace.World.StarSystem.Presentation.Ui;
using GrimSpace.World.StarSystem.Resources;
using RunState = GrimSpace.Run.State;

namespace GrimSpace.Tests.World.StarSystem.Events;

[StarSystemTestSuite]
public sealed class WorldEventHudTests
{
	[Fact]
	public void LoadedPendingEvent_OpensWithItsPresentation()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pending = OpenAnnouncement(run);
		var hud = new RecordingWorldEventHud();

		using var _ = Attach(run, hud);

		Assert.Equal(pending.Presentation, hud.Presentation);
		Assert.True(hud.IsOpen);
	}

	[Fact]
	public void Impact_OpensWithThatPresentation()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var hud = new RecordingWorldEventHud();
		using var _ = Attach(run, hud);
		Assert.False(hud.IsOpen);

		var pending = OpenAnnouncement(run);

		Assert.Equal(pending.Presentation, hud.Presentation);
		Assert.True(hud.IsOpen);
	}

	[Fact]
	public void Dismiss_EnqueuesAcknowledgeAndLeavesHullAndPromptUntouched()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pending = OpenAnnouncement(run);
		var before = HullOf(run, pending.ShipId);
		var hud = new RecordingWorldEventHud();
		IAction? enqueued = null;

		using var _ = Attach(run, hud, action =>
		{
			enqueued = action;
			return true;
		});
		hud.RequestDismiss();

		Assert.Equal(
			new AcknowledgeAsteroidImpactAction(RunState.PlayerFleetUnitId, pending.Id),
			enqueued);
		Assert.Equal(before, HullOf(run, pending.ShipId));
		Assert.Equal(pending, run.StarSystem.Map.PendingWorldEvent);
		Assert.True(run.StarSystem.Map.WaitingForPlayerInput);
		Assert.False(hud.IsOpen);
	}

	[Fact]
	public void Dismiss_Salvage_EnqueuesSalvageAcknowledge()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pending = OpenSalvage(run);
		var hud = new RecordingWorldEventHud();
		IAction? enqueued = null;

		using var _ = Attach(run, hud, action =>
		{
			enqueued = action;
			return true;
		});
		hud.RequestDismiss();

		Assert.Equal(
			new AcknowledgeDriftSalvageAction(RunState.PlayerFleetUnitId, pending.Id),
			enqueued);
		Assert.Equal(pending.Reward, hud.Reward);
		Assert.Equal(pending, run.StarSystem.Map.PendingWorldEvent);
		Assert.False(hud.IsOpen);
	}

	[Fact]
	public void RejectedDismiss_ClosesTheShellAndLeavesThePrompt()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pending = OpenAnnouncement(run);
		var hud = new RecordingWorldEventHud();
		string? summary = null;

		using var _ = Attach(
			run,
			hud,
			_ => false,
			(message, _) => summary = message);
		hud.RequestDismiss();

		Assert.False(hud.IsOpen);
		Assert.Equal(pending, run.StarSystem.Map.PendingWorldEvent);
		Assert.True(run.StarSystem.Map.WaitingForPlayerInput);
		Assert.Equal("Could not acknowledge the event.", summary);
	}

	private static WorldEventController Attach(
		RunState run,
		RecordingWorldEventHud hud,
		Func<IAction, bool>? tryEnqueue = null,
		Action<string, string>? presentError = null) =>
		new(
			hud,
			() => run.StarSystem.Map.PendingWorldEvent,
			tryEnqueue ?? (_ => false),
			sync => run.StarSystem.Subscribe<AsteroidImpactAction>(_ => sync()),
			presentError ?? ((_, _) => { }));

	private static PendingWorldEvent OpenAnnouncement(RunState run)
	{
		var pilot = run.PilotRegistry.Active[0];
		var pending = new PendingWorldEvent(
			"world-event-test",
			EWorldEventId.AsteroidAccident,
			pilot.Id,
			pilot.AssignedShipId!,
			WorldEventCatalog.Asteroid.Damage,
			new WorldEventPresentation("Asteroid Strike", "Test body."));
		run.StarSystem.CommitSetup(new AsteroidImpactAction(StarSystemActorIds.WorldEvents, pending));
		return pending;
	}

	private static PendingWorldEvent OpenSalvage(RunState run)
	{
		var pilot = run.PilotRegistry.Active[0];
		var pending = new PendingWorldEvent(
			"world-event-salvage",
			EWorldEventId.DriftSalvage,
			pilot.Id,
			pilot.AssignedShipId!,
			0,
			new WorldEventPresentation("Combed a Derelict", "Test body."),
			ResourceBundle.Of(ResourceId.ScrapAlloy, WorldEventCatalog.Drift.ScrapMin));
		run.StarSystem.CommitSetup(new DriftSalvageFoundAction(StarSystemActorIds.WorldEvents, pending));
		return pending;
	}

	private static int HullOf(RunState run, string shipId) =>
		run.ItemInventory.Get(shipId).CurrentHullPoints;

	private sealed class RecordingWorldEventHud : IWorldEventHud
	{
		public WorldEventPresentation? Presentation { get; private set; }

		public bool IsOpen { get; private set; }

		public event Action? DismissRequested;

		public void Sync(WorldEventPresentation presentation, ResourceBundle? reward = null)
		{
			Presentation = presentation;
			Reward = reward;
			IsOpen = true;
		}

		public ResourceBundle? Reward { get; private set; }

		public void Close() => IsOpen = false;

		public void SetBusy(bool _)
		{
		}

		public bool TryHandleInput(Godot.InputEvent @event) => false;

		public void RequestDismiss() => DismissRequested?.Invoke();
	}
}
