using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Run.Persistence;
using GrimSpace.Tests.Run;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Events;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Resources;
using RunState = GrimSpace.Run.State;

namespace GrimSpace.Tests.World.StarSystem.Events;

[StarSystemTestSuite]
public sealed class DriftSalvageTests
{
	[Theory]
	[InlineData(5, 0.20)]
	[InlineData(0, 0)]
	[InlineData(-5, 0)]
	public void TriggerChance_RisesWithDeliberation(int deliberation, double expected) =>
		Assert.Equal(
			expected,
			RollForDriftSalvageDef.TriggerChance(PilotWith(deliberation)),
			precision: 10);

	[Theory]
	[InlineData(5, 0.18, 0.22)]
	[InlineData(-5, 0, 0)]
	public void SelectFinder_TriggersAtPersonalityAdjustedRate(
		int deliberation,
		double minRate,
		double maxRate)
	{
		var pilots = new[] { PilotWith(deliberation) };

		var triggers = Enumerable.Range(0, 5_000)
			.Count(seed => RollForDriftSalvageDef.SelectFinder((ulong)seed, pilots) is not null);

		Assert.InRange(triggers / 5_000.0, minRate, maxRate);
	}

	[Fact]
	public void SelectFinder_IsDeterministicForSeed()
	{
		var pilots = new[] { PilotWith(5, "a"), PilotWith(5, "b"), PilotWith(5, "c") };

		var results = Enumerable.Range(0, 200)
			.Select(seed => RollForDriftSalvageDef.SelectFinder((ulong)seed, pilots)?.Name)
			.ToArray();
		var repeated = Enumerable.Range(0, 200)
			.Select(seed => RollForDriftSalvageDef.SelectFinder((ulong)seed, pilots)?.Name)
			.ToArray();

		Assert.Equal(results, repeated);
		Assert.Contains("a", results);
		Assert.Contains("b", results);
		Assert.Contains("c", results);
	}

	[Fact]
	public void SelectFinder_WithoutPilots_ReturnsNull() =>
		Assert.Null(RollForDriftSalvageDef.SelectFinder(1, Array.Empty<Pilot>()));

	[Fact]
	public void RollReward_IsUsuallyScrapAndRarelyACore()
	{
		var rewards = Enumerable.Range(0, 5_000)
			.Select(seed => RollForDriftSalvageDef.RollReward((ulong)seed))
			.ToArray();
		var cores = rewards.Count(reward =>
			reward.TryGet(ResourceId.IndustrialCore, out var amount) && amount == 1);

		Assert.InRange(cores / 5_000.0, 0.01, 0.03);
		var scrapHauls = rewards
			.Select(reward => reward.TryGet(ResourceId.ScrapAlloy, out var scrap) ? scrap : 0)
			.Where(scrap => scrap > 0)
			.ToArray();
		Assert.InRange(scrapHauls.Average(), 110, 130);
		Assert.True(scrapHauls.Distinct().Count() > 1);
		Assert.All(rewards, reward =>
		{
			var scrap = reward.TryGet(ResourceId.ScrapAlloy, out var scrapAmount) ? scrapAmount : 0;
			var core = reward.TryGet(ResourceId.IndustrialCore, out var coreAmount) ? coreAmount : 0;
			Assert.True(
				(scrap >= WorldEventCatalog.Drift.ScrapMin
					&& scrap <= WorldEventCatalog.Drift.ScrapMax
					&& core == 0)
				|| (scrap == 0 && core == 1));
		});
	}

	[Fact]
	public void Roll_WhileTraveling_OpensAnnouncementOnNextTick()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		FieldPilot(run, deliberation: 5);
		StartJourney(run);
		var roll = TriggeringRoll(run);
		var finder = RollForDriftSalvageDef.SelectFinder(
			roll.RngSeed,
			WorldEventContexts.RosterPilots(run.StarSystem.Map))!;
		var reward = RollForDriftSalvageDef.RollReward(roll.RngSeed);

		run.StarSystem.CommitSetup(roll);
		Assert.True(run.StarSystem.Map.Timeline.ContainsPending(action => action is DriftSalvageFoundAction));
		Assert.Null(run.StarSystem.Map.PendingWorldEvent);

		run.StarSystem.AdvanceTick();

		var pending = run.StarSystem.Map.PendingWorldEvent;
		Assert.NotNull(pending);
		Assert.Equal(EWorldEventId.DriftSalvage, pending.EventId);
		Assert.Equal(finder.Id, pending.PilotId);
		Assert.Equal(reward, pending.Reward);
		Assert.Equal(WorldEventCatalog.Drift.Announce(finder.Name), pending.Presentation);
		Assert.True(run.StarSystem.Map.WaitingForPlayerInput);
	}

	[Fact]
	public void Roll_ShiplessPilotTravelingWithFleet_CanFindSalvage()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		foreach (var assigned in run.PilotRegistry.Assigned)
			run.PilotRegistry.Update(assigned.WithPersonality(new PilotPersonality(0, -5, 0)));
		var passenger = PilotWith(5, "Passenger");
		run.PilotRegistry.Register(passenger);
		StartJourney(run);
		var roll = Enumerable.Range(0, 10_000)
			.Select(seed => new RollForDriftSalvageAction(StarSystemActorIds.WorldEvents, (ulong)seed))
			.First(candidate => RollForDriftSalvageDef.SelectFinder(
				candidate.RngSeed,
				WorldEventContexts.RosterPilots(run.StarSystem.Map))?.Id == passenger.Id);

		run.StarSystem.CommitSetup(roll);
		run.StarSystem.AdvanceTick();

		var pending = run.StarSystem.Map.PendingWorldEvent;
		Assert.NotNull(pending);
		Assert.Equal(passenger.Id, pending.PilotId);
		Assert.Null(pending.ShipId);
	}

	[Fact]
	public void Roll_WhileDocked_PassesSilently()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		FieldPilot(run, deliberation: 5);
		var roll = TriggeringRoll(run);

		run.StarSystem.CommitSetup(roll);
		run.StarSystem.AdvanceTick();

		Assert.False(run.StarSystem.Map.Timeline.ContainsPending(action => action is DriftSalvageFoundAction));
		Assert.Null(run.StarSystem.Map.PendingWorldEvent);
		Assert.False(run.StarSystem.Map.WaitingForPlayerInput);
	}

	[Fact]
	public void Roll_ForAnImpulsivePilot_PassesSilently()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pilot = FieldPilot(run, deliberation: -5);
		foreach (var other in run.PilotRegistry.Assigned.Where(active => active.Id != pilot.Id).ToArray())
			run.PilotRegistry.Unassign(other.Id);
		StartJourney(run);

		run.StarSystem.CommitSetup(new RollForDriftSalvageAction(StarSystemActorIds.WorldEvents, 1));
		run.StarSystem.AdvanceTick();

		Assert.False(run.StarSystem.Map.Timeline.ContainsPending(action => action is DriftSalvageFoundAction));
		Assert.Null(run.StarSystem.Map.PendingWorldEvent);
	}

	[Fact]
	public void Roll_WhileAnotherEventIsPending_PassesSilently()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		FieldPilot(run, deliberation: 5);
		StartJourney(run);
		OpenAnnouncement(run, ScrapReward());

		run.StarSystem.CommitSetup(TriggeringRoll(run));

		Assert.False(run.StarSystem.Map.Timeline.ContainsPending(action => action is DriftSalvageFoundAction));
	}

	[Fact]
	public void Acknowledge_GrantsTheAnnouncedHaulOnceAndReleasesTheWait()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var reward = ScrapReward();
		var pending = OpenAnnouncement(run, reward);
		var before = Balance(run, ResourceId.ScrapAlloy);

		run.StarSystem.CommitSetup(Acknowledge(pending));

		Assert.Equal(before + ScrapOf(reward), Balance(run, ResourceId.ScrapAlloy));
		Assert.Equal(0, Balance(run, ResourceId.IndustrialCore));
		Assert.Null(run.StarSystem.Map.PendingWorldEvent);
		Assert.False(run.StarSystem.Map.WaitingForPlayerInput);

		run.StarSystem.CommitSetup(Acknowledge(pending));

		Assert.Equal(before + ScrapOf(reward), Balance(run, ResourceId.ScrapAlloy));
	}

	[Fact]
	public void Acknowledge_GrantsAnIndustrialCoreWhenThatWasRolled()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var reward = ResourceBundle.Of(ResourceId.IndustrialCore, 1);
		var pending = OpenAnnouncement(run, reward);
		var scrap = Balance(run, ResourceId.ScrapAlloy);

		run.StarSystem.CommitSetup(Acknowledge(pending));

		Assert.Equal(1, Balance(run, ResourceId.IndustrialCore));
		Assert.Equal(scrap, Balance(run, ResourceId.ScrapAlloy));
		Assert.Null(run.StarSystem.Map.PendingWorldEvent);
	}

	[Fact]
	public void Acknowledge_BeforeAnnouncementOrWithWrongId_DoesNothing()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pending = OpenAnnouncement(run, ScrapReward());
		var before = Balance(run, ResourceId.ScrapAlloy);

		run.StarSystem.CommitSetup(new AcknowledgeDriftSalvageAction(
			RunState.PlayerFleetUnitId,
			"world-event-stale"));

		Assert.Equal(before, Balance(run, ResourceId.ScrapAlloy));
		Assert.Equal(pending, run.StarSystem.Map.PendingWorldEvent);
		Assert.True(run.StarSystem.Map.WaitingForPlayerInput);
	}

	[Fact]
	public void Fork_PreservesPendingEvent()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pending = OpenAnnouncement(run, ScrapReward());

		var fork = run.StarSystem.Map.ForkForSimulation();

		Assert.Equal(pending, fork.PendingWorldEvent);
		Assert.True(fork.WaitingForPlayerInput);
	}

	[Fact]
	public void SaveLoad_PreservesPendingEventAndStillGrantsOnce()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pending = OpenAnnouncement(run, ScrapReward());
		var registry = PersistenceRegistry.CreateDefault();

		var snapshot = run.CaptureSnapshot(registry);
		using var restored = RunState.FromSnapshot(snapshot, registry);

		Assert.Equal(pending, restored.StarSystem.Map.PendingWorldEvent);
		Assert.True(restored.StarSystem.Map.WaitingForPlayerInput);
		var before = Balance(restored, ResourceId.ScrapAlloy);

		restored.StarSystem.CommitSetup(Acknowledge(pending));
		restored.StarSystem.CommitSetup(Acknowledge(pending));

		Assert.Equal(before + ScrapOf(pending.Reward!), Balance(restored, ResourceId.ScrapAlloy));
		Assert.Null(restored.StarSystem.Map.PendingWorldEvent);
	}

	private static Pilot PilotWith(int deliberation, string name = "pilot") =>
		Pilot.FromRecruitProfile(
			null,
			new PilotRecruitProfile(
				name,
				"pilot_01",
				EPilotBackgroundId.HydraulicAbuser,
				personality: new PilotPersonality(0, deliberation, 0)));

	private static Pilot FieldPilot(RunState run, int deliberation)
	{
		var ranger = RunTestKit.StarterRangerPilot(run);
		var shipId = ranger.AssignedShipId!;
		run.PilotRegistry.Unassign(ranger.Id);
		var pilot = PilotWith(deliberation, "Keel");
		run.PilotRegistry.Register(pilot);
		run.PilotRegistry.AssignShip(
			pilot.Id,
			shipId,
			run.ItemInventory,
			RunState.PlayerFleetUnitId);
		return run.PilotRegistry.Assigned.Single(active => active.Id == pilot.Id);
	}

	private static void StartJourney(RunState run)
	{
		var destination = run.StarSystem.Map.DocksByPoiId[SupplySystemPlan.Copper.StoragePoiId].Position;
		run.StarSystem.PlayerAgent!.TryQueueMove(destination);
		run.StarSystem.AdvanceTick();
	}

	private static RollForDriftSalvageAction TriggeringRoll(RunState run)
	{
		var pilots = WorldEventContexts.RosterPilots(run.StarSystem.Map).ToArray();
		for (ulong seed = 0; seed < 10_000; seed++)
		{
			if (RollForDriftSalvageDef.SelectFinder(seed, pilots) is not null)
				return new RollForDriftSalvageAction(StarSystemActorIds.WorldEvents, seed);
		}

		throw new InvalidOperationException("No seed triggered drift salvage.");
	}

	private static ResourceBundle ScrapReward() =>
		ResourceBundle.Of(ResourceId.ScrapAlloy, WorldEventCatalog.Drift.ScrapMin);

	private static int ScrapOf(ResourceBundle reward) =>
		reward.TryGet(ResourceId.ScrapAlloy, out var scrap) ? scrap : 0;

	private static PendingWorldEvent OpenAnnouncement(RunState run, ResourceBundle reward)
	{
		var pilot = run.PilotRegistry.Assigned[0];
		var pending = new PendingWorldEvent(
			"world-event-test",
			EWorldEventId.DriftSalvage,
			pilot.Id,
			pilot.AssignedShipId!,
			0,
			WorldEventCatalog.Drift.Announce(pilot.Name),
			reward);
		run.StarSystem.CommitSetup(new DriftSalvageFoundAction(StarSystemActorIds.WorldEvents, pending));
		Assert.Equal(pending, run.StarSystem.Map.PendingWorldEvent);
		return pending;
	}

	private static AcknowledgeDriftSalvageAction Acknowledge(PendingWorldEvent pending) =>
		new(RunState.PlayerFleetUnitId, pending.Id);

	private static int Balance(RunState run, ResourceId id) =>
		run.StarSystem.Map.PlayerResources.GetBalance(id);
}
