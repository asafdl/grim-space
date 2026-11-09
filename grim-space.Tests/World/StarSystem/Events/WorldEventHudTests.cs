using GrimSpace.Core.Actions;
using GrimSpace.Pilots;
using GrimSpace.Tests.Run;
using GrimSpace.Units.Enums;
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
		Assert.Null(hud.Recruit);
		Assert.Equal(pending, run.StarSystem.Map.PendingWorldEvent);
		Assert.False(hud.IsOpen);
	}

	[Fact]
	public void Dismiss_DistressLoot_EnqueuesLootAcknowledge()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pending = OpenDistressLoot(run);
		var hud = new RecordingWorldEventHud();
		IAction? enqueued = null;

		using var _ = Attach(run, hud, action =>
		{
			enqueued = action;
			return true;
		});
		hud.RequestDismiss();

		Assert.Equal(
			new AcknowledgeDistressCallAction(RunState.PlayerFleetUnitId, pending.Id),
			enqueued);
		Assert.Equal(pending.Reward, hud.Reward);
		Assert.Null(hud.Recruit);
		Assert.False(hud.IsOpen);
	}

	[Fact]
	public void AcceptAndReject_DistressRecruit_EnqueueThoseActions()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pending = OpenDistressRecruit(run);
		var hud = new RecordingWorldEventHud();
		IAction? enqueued = null;

		using var _ = Attach(run, hud, action =>
		{
			enqueued = action;
			return true;
		});

		Assert.Equal(pending.Recruit, hud.Recruit);
		Assert.Null(hud.Reward);
		hud.RequestReject();
		Assert.Equal(
			new RejectDistressRecruitAction(RunState.PlayerFleetUnitId, pending.Id),
			enqueued);

		hud.RequestAccept();
		Assert.Equal(
			new AcceptDistressRecruitAction(RunState.PlayerFleetUnitId, pending.Id),
			enqueued);
	}

	[Fact]
	public void PayAndWalk_Stake_EnqueueThoseActions()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pending = OpenStake(run);
		var hud = new RecordingWorldEventHud();
		IAction? enqueued = null;

		using var _ = Attach(run, hud, action =>
		{
			enqueued = action;
			return true;
		});

		Assert.True(hud.StakeChoice);
		Assert.Equal(pending.Reward, hud.Reward);
		hud.RequestReject();
		Assert.Equal(
			new RefuseCompulsiveStakeAction(RunState.PlayerFleetUnitId, pending.Id),
			enqueued);

		hud.RequestAccept();
		Assert.Equal(
			new PayCompulsiveStakeAction(RunState.PlayerFleetUnitId, pending.Id),
			enqueued);
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
			sync => WorldEventController.Join(
				run.StarSystem.Subscribe<AsteroidImpactAction>(_ => sync()),
				run.StarSystem.Subscribe<DriftSalvageFoundAction>(_ => sync()),
				run.StarSystem.Subscribe<DistressCallReceivedAction>(_ => sync()),
				run.StarSystem.Subscribe<CompulsiveStakeDemandedAction>(_ => sync())),
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

	private static PendingWorldEvent OpenDistressLoot(RunState run)
	{
		var pilot = run.PilotRegistry.Active[0];
		var pending = new PendingWorldEvent(
			"world-event-distress-loot",
			EWorldEventId.DistressCall,
			pilot.Id,
			pilot.AssignedShipId!,
			0,
			new WorldEventPresentation("Killed the Caller", "Test body."),
			ResourceBundle.Create(
				(ResourceId.ScrapAlloy, WorldEventCatalog.Distress.ScrapMin),
				(ResourceId.Credits, WorldEventCatalog.Distress.CreditsMin)));
		run.StarSystem.CommitSetup(new DistressCallReceivedAction(StarSystemActorIds.WorldEvents, pending));
		return pending;
	}

	private static PendingWorldEvent OpenDistressRecruit(RunState run)
	{
		var pilot = run.PilotRegistry.Active[0];
		var pending = new PendingWorldEvent(
			"world-event-distress-recruit",
			EWorldEventId.DistressCall,
			pilot.Id,
			pilot.AssignedShipId!,
			0,
			new WorldEventPresentation("Survivor Wants Aboard", "Test body."),
			Recruit: new PilotRecruitProfile(
				"Survivor",
				"pilot_02",
				EPilotBackgroundId.HydraulicAbuser,
				classId: EOperatorClassId.Support));
		run.StarSystem.CommitSetup(new DistressCallReceivedAction(StarSystemActorIds.WorldEvents, pending));
		return pending;
	}

	private static PendingWorldEvent OpenStake(RunState run)
	{
		var pilot = run.PilotRegistry.Active[0];
		var pending = new PendingWorldEvent(
			"world-event-stake",
			EWorldEventId.CompulsiveStake,
			pilot.Id,
			pilot.AssignedShipId!,
			0,
			new WorldEventPresentation("Wants a Stake", "Test body."),
			ResourceBundle.Of(ResourceId.Credits, -WorldEventCatalog.Stake.CreditsMin));
		run.StarSystem.CommitSetup(new CompulsiveStakeDemandedAction(StarSystemActorIds.WorldEvents, pending));
		return pending;
	}

	private static int HullOf(RunState run, string shipId) =>
		run.ItemInventory.Get(shipId).CurrentHullPoints;

	private sealed class RecordingWorldEventHud : IWorldEventHud
	{
		public WorldEventPresentation? Presentation { get; private set; }

		public bool IsOpen { get; private set; }

		public event Action? DismissRequested;

		public event Action? AcceptRequested;

		public event Action? RejectRequested;

		public void Sync(
			WorldEventPresentation presentation,
			ResourceBundle? reward = null,
			PilotRecruitProfile? recruit = null,
			bool stakeChoice = false)
		{
			Presentation = presentation;
			Reward = reward;
			Recruit = recruit;
			StakeChoice = stakeChoice;
			IsOpen = true;
		}

		public bool StakeChoice { get; private set; }

		public ResourceBundle? Reward { get; private set; }

		public PilotRecruitProfile? Recruit { get; private set; }

		public void Close() => IsOpen = false;

		public void SetBusy(bool _)
		{
		}

		public bool TryHandleInput(Godot.InputEvent @event) => false;

		public void RequestDismiss() => DismissRequested?.Invoke();

		public void RequestAccept() => AcceptRequested?.Invoke();

		public void RequestReject() => RejectRequested?.Invoke();
	}
}
