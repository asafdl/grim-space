using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Run.Persistence;
using GrimSpace.Tests.Run;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Events;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Resources;
using RunState = GrimSpace.Run.State;

namespace GrimSpace.Tests.World.StarSystem.Events;

[StarSystemTestSuite]
public sealed class DistressCallTests
{
	[Theory]
	[InlineData(5, true, 0.20)]
	[InlineData(-5, false, 0.20)]
	[InlineData(5, false, 0)]
	[InlineData(-5, true, 0)]
	[InlineData(0, true, 0)]
	public void TriggerChance_RisesTowardBothPoles(int allegiance, bool positiveSide, double expected) =>
		Assert.Equal(
			expected,
			RollForDistressCallDef.TriggerChance(PilotWith(allegiance), positiveSide),
			precision: 10);

	[Theory]
	[InlineData(5, 0.18, 0.22)]
	[InlineData(-5, 0.18, 0.22)]
	[InlineData(0, 0, 0)]
	public void SelectResponder_TriggersAtPersonalityAdjustedRate(
		int allegiance,
		double minRate,
		double maxRate)
	{
		var pilots = new[] { PilotWith(allegiance) };

		var triggers = Enumerable.Range(0, 5_000)
			.Count(seed => RollForDistressCallDef.SelectResponder((ulong)seed, pilots) is not null);

		Assert.InRange(triggers / 5_000.0, minRate, maxRate);
	}

	[Fact]
	public void SelectResponder_AllowsBothLoyalAndSelfishPilots()
	{
		var pilots = new[] { PilotWith(5, "loyal"), PilotWith(-5, "selfish") };

		var names = Enumerable.Range(0, 2_000)
			.Select(seed => RollForDistressCallDef.SelectResponder((ulong)seed, pilots)?.Name)
			.Where(name => name is not null)
			.ToArray();

		Assert.Contains("loyal", names);
		Assert.Contains("selfish", names);
	}

	[Fact]
	public void SelectResponder_FullRosterSkipsLoyalPilotsButAllowsLoot()
	{
		var pilots = new[] { PilotWith(5, "loyal"), PilotWith(-5, "selfish") };
		var names = Enumerable.Range(0, 2_000)
			.Select(seed => RollForDistressCallDef.SelectResponder((ulong)seed, pilots, rosterHasRoom: false)?.Name)
			.Where(name => name is not null)
			.ToArray();

		Assert.NotEmpty(names);
		Assert.All(names, name => Assert.Equal("selfish", name));
	}

	[Fact]
	public void RollReward_PaysCreditsAndScrap()
	{
		var rewards = Enumerable.Range(0, 2_000)
			.Select(seed => RollForDistressCallDef.RollReward((ulong)seed))
			.ToArray();

		Assert.All(rewards, reward =>
		{
			Assert.True(reward.TryGet(ResourceId.Credits, out var credits));
			Assert.True(reward.TryGet(ResourceId.ScrapAlloy, out var scrap));
			Assert.InRange(credits, WorldEventCatalog.Distress.CreditsMin, WorldEventCatalog.Distress.CreditsMax);
			Assert.InRange(scrap, WorldEventCatalog.Distress.ScrapMin, WorldEventCatalog.Distress.ScrapMax);
		});
		Assert.InRange(
			rewards.Average(reward => reward.TryGet(ResourceId.Credits, out var credits) ? credits : 0),
			60,
			80);
	}

	[Fact]
	public void RollRecruit_IsDeterministicAndRecruitable()
	{
		var first = RollForDistressCallDef.RollRecruit(17);
		var second = RollForDistressCallDef.RollRecruit(17);

		Assert.Equal(first.Name, second.Name);
		Assert.Equal(first.PortraitId, second.PortraitId);
		Assert.Equal(first.BackgroundId, second.BackgroundId);
		Assert.Equal(first.ClassId, second.ClassId);
		Assert.Equal(first.BaseSalaryCredits, second.BaseSalaryCredits);
		Assert.Contains(first.BackgroundId, PilotRecruitment.RecruitableBackgroundIds);
		Assert.Contains(first.ClassId, PilotRecruitment.RecruitableOperatorClassIds);
		Assert.NotEqual(first.Name, RollForDistressCallDef.RollRecruit(18).Name);
	}

	[Fact]
	public void Roll_WhileTraveling_LoyalPilotOffersARecruit()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		FieldOnly(run, allegiance: 5);
		StartJourney(run);
		var roll = TriggeringRoll(run);
		var responder = Responder(run, roll.RngSeed)!;

		run.StarSystem.CommitSetup(roll);
		run.StarSystem.AdvanceTick();

		var pending = run.StarSystem.Map.PendingWorldEvent;
		Assert.NotNull(pending);
		Assert.Equal(EWorldEventId.DistressCall, pending.EventId);
		Assert.Equal(responder.Id, pending.PilotId);
		Assert.Null(pending.Reward);
		Assert.NotNull(pending.Recruit);
		Assert.Equal(
			WorldEventCatalog.Distress.AnnounceSurvivor(responder.Name, pending.Recruit.Name),
			pending.Presentation);
		Assert.True(run.StarSystem.Map.WaitingForPlayerInput);
	}

	[Fact]
	public void Roll_WhileTraveling_SelfishPilotOffersLoot()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		FieldOnly(run, allegiance: -5);
		StartJourney(run);
		var roll = TriggeringRoll(run);
		var responder = Responder(run, roll.RngSeed)!;

		run.StarSystem.CommitSetup(roll);
		run.StarSystem.AdvanceTick();

		var pending = run.StarSystem.Map.PendingWorldEvent;
		Assert.NotNull(pending);
		Assert.Equal(responder.Id, pending.PilotId);
		Assert.Null(pending.Recruit);
		Assert.Equal(RollForDistressCallDef.RollReward(roll.RngSeed), pending.Reward);
		Assert.Equal(WorldEventCatalog.Distress.AnnounceKill(responder.Name), pending.Presentation);
	}

	[Fact]
	public void Roll_WhileDocked_PassesSilently()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		FieldOnly(run, allegiance: -5);
		var roll = TriggeringRoll(run);

		run.StarSystem.CommitSetup(roll);
		run.StarSystem.AdvanceTick();

		Assert.Null(run.StarSystem.Map.PendingWorldEvent);
		Assert.False(run.StarSystem.Map.WaitingForPlayerInput);
	}

	[Fact]
	public void Roll_ForANeutralPilot_PassesSilently()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		FieldOnly(run, allegiance: 0);
		StartJourney(run);

		run.StarSystem.CommitSetup(new RollForDistressCallAction(StarSystemActorIds.WorldEvents, 1));
		run.StarSystem.AdvanceTick();

		Assert.Null(run.StarSystem.Map.PendingWorldEvent);
	}

	[Fact]
	public void Roll_LoyalPilotWithMoreThanFourPilots_OffersRecruit()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var loyal = FieldOnly(run, allegiance: 5);
		StartJourney(run);
		AddReservePilots(run);
		var roll = TriggeringRoll(run, loyal.Id);

		run.StarSystem.CommitSetup(roll);
		run.StarSystem.AdvanceTick();

		Assert.True(run.PilotRegistry.All.Count > 4);
		Assert.NotNull(run.StarSystem.Map.PendingWorldEvent);
		Assert.NotNull(run.StarSystem.Map.PendingWorldEvent.Recruit);
		Assert.Null(run.StarSystem.Map.PendingWorldEvent.Reward);
	}

	[Fact]
	public void Roll_SelfishPilotWithMoreThanFourPilots_StillOffersLoot()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		FieldOnly(run, allegiance: -5);
		AddReservePilots(run);
		StartJourney(run);
		var roll = TriggeringRoll(run);

		run.StarSystem.CommitSetup(roll);
		run.StarSystem.AdvanceTick();

		Assert.NotNull(run.StarSystem.Map.PendingWorldEvent);
		Assert.NotNull(run.StarSystem.Map.PendingWorldEvent!.Reward);
		Assert.Null(run.StarSystem.Map.PendingWorldEvent.Recruit);
	}

	[Fact]
	public void Acknowledge_GrantsLootOnce()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var reward = RollForDistressCallDef.RollReward(4);
		var pending = OpenLoot(run, reward);
		var credits = Balance(run, ResourceId.Credits);
		var scrap = Balance(run, ResourceId.ScrapAlloy);
		var fuel = Balance(run, ResourceId.Fuel);

		run.StarSystem.CommitSetup(Acknowledge(pending));
		run.StarSystem.CommitSetup(Acknowledge(pending));

		Assert.Equal(credits + Amount(reward, ResourceId.Credits), Balance(run, ResourceId.Credits));
		Assert.Equal(scrap + Amount(reward, ResourceId.ScrapAlloy), Balance(run, ResourceId.ScrapAlloy));
		Assert.Equal(fuel, Balance(run, ResourceId.Fuel));
		Assert.Null(run.StarSystem.Map.PendingWorldEvent);
		Assert.False(run.StarSystem.Map.WaitingForPlayerInput);
	}

	[Fact]
	public void Accept_RegistersTheSurvivor()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pending = OpenRecruit(run);
		var before = run.PilotRegistry.All.Count;
		var fuel = Balance(run, ResourceId.Fuel);

		run.StarSystem.CommitSetup(Accept(pending));
		run.StarSystem.CommitSetup(Accept(pending));

		Assert.Equal(before + 1, run.PilotRegistry.All.Count);
		Assert.Contains(run.PilotRegistry.All, pilot => pilot.Name == pending.Recruit!.Name);
		Assert.Equal(fuel, Balance(run, ResourceId.Fuel));
		Assert.Null(run.StarSystem.Map.PendingWorldEvent);
	}

	[Fact]
	public void Accept_WithMoreThanFourPilots_RegistersSurvivorOnce()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pending = OpenRecruit(run);
		AddReservePilots(run);
		var fuel = Balance(run, ResourceId.Fuel);
		var count = run.PilotRegistry.All.Count;

		run.StarSystem.CommitSetup(Accept(pending));
		run.StarSystem.CommitSetup(Accept(pending));

		Assert.True(count > 4);
		Assert.Equal(count + 1, run.PilotRegistry.All.Count);
		Assert.Contains(run.PilotRegistry.All, pilot => pilot.Name == pending.Recruit!.Name);
		Assert.Equal(fuel, Balance(run, ResourceId.Fuel));
		Assert.Null(run.StarSystem.Map.PendingWorldEvent);
		Assert.False(run.StarSystem.Map.WaitingForPlayerInput);
	}

	[Fact]
	public void Accept_WithTenPilots_DoesNotExceedCapOrConsumePendingRecruit()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pending = OpenRecruit(run);
		while (run.PilotRegistry.All.Count < 10)
			run.PilotRegistry.Register(PilotWith(0, $"bench-{run.PilotRegistry.All.Count}"));
		var fuel = Balance(run, ResourceId.Fuel);

		run.StarSystem.CommitSetup(Accept(pending));

		Assert.Equal(10, run.PilotRegistry.All.Count);
		Assert.Equal(fuel, Balance(run, ResourceId.Fuel));
		Assert.Equal(pending, run.StarSystem.Map.PendingWorldEvent);
		Assert.True(run.StarSystem.Map.WaitingForPlayerInput);
	}

	[Fact]
	public void Reject_LeavesTheRoster()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pending = OpenRecruit(run);
		var before = run.PilotRegistry.All.Select(pilot => pilot.Id).ToArray();
		var fuel = Balance(run, ResourceId.Fuel);

		run.StarSystem.CommitSetup(Reject(pending));
		run.StarSystem.CommitSetup(Reject(pending));

		Assert.Equal(before, run.PilotRegistry.All.Select(pilot => pilot.Id));
		Assert.Equal(fuel, Balance(run, ResourceId.Fuel));
		Assert.Null(run.StarSystem.Map.PendingWorldEvent);
	}

	[Fact]
	public void SaveLoad_PreservesLootAndStillPaysOnce()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pending = OpenLoot(run, RollForDistressCallDef.RollReward(9));
		var registry = PersistenceRegistry.CreateDefault();

		var snapshot = run.CaptureSnapshot(registry);
		using var restored = RunState.FromSnapshot(snapshot, registry);

		Assert.Equal(pending, restored.StarSystem.Map.PendingWorldEvent);
		var credits = Balance(restored, ResourceId.Credits);
		var fuel = Balance(restored, ResourceId.Fuel);

		restored.StarSystem.CommitSetup(Acknowledge(pending));
		restored.StarSystem.CommitSetup(Acknowledge(pending));

		Assert.Equal(credits + Amount(pending.Reward!, ResourceId.Credits), Balance(restored, ResourceId.Credits));
		Assert.Equal(fuel, Balance(restored, ResourceId.Fuel));
		Assert.Null(restored.StarSystem.Map.PendingWorldEvent);
	}

	[Fact]
	public void SaveLoad_PreservesTheSurvivorAndStillRecruitsOnce()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pending = OpenRecruit(run);
		var registry = PersistenceRegistry.CreateDefault();

		var snapshot = run.CaptureSnapshot(registry);
		using var restored = RunState.FromSnapshot(snapshot, registry);
		var restoredPending = restored.StarSystem.Map.PendingWorldEvent;

		Assert.NotNull(restoredPending);
		Assert.Equal(pending.Presentation, restoredPending.Presentation);
		Assert.Equal(pending.Recruit!.Name, restoredPending.Recruit!.Name);
		Assert.Equal(pending.Recruit.ClassId, restoredPending.Recruit.ClassId);
		Assert.Equal(pending.Recruit.BackgroundId, restoredPending.Recruit.BackgroundId);
		Assert.Equal(pending.Recruit.PortraitId, restoredPending.Recruit.PortraitId);
		Assert.Equal(pending.Recruit.BaseSalaryCredits, restoredPending.Recruit.BaseSalaryCredits);
		var before = restored.PilotRegistry.All.Count;

		restored.StarSystem.CommitSetup(Accept(pending));
		restored.StarSystem.CommitSetup(Accept(pending));

		Assert.Equal(before + 1, restored.PilotRegistry.All.Count);
		Assert.Contains(restored.PilotRegistry.All, pilot => pilot.Name == pending.Recruit.Name);
	}

	private static Pilot PilotWith(int allegiance, string name = "pilot") =>
		Pilot.FromRecruitProfile(
			null,
			new PilotRecruitProfile(
				name,
				"pilot_01",
				EPilotBackgroundId.HydraulicAbuser,
				personality: new PilotPersonality(0, 0, allegiance)));

	private static Pilot FieldOnly(RunState run, int allegiance)
	{
		var ranger = RunTestKit.StarterRangerPilot(run);
		var shipId = ranger.AssignedShipId!;
		run.PilotRegistry.Unassign(ranger.Id);
		var pilot = PilotWith(allegiance, "Keel");
		run.PilotRegistry.Register(pilot);
		run.PilotRegistry.AssignShip(
			pilot.Id,
			shipId,
			run.ItemInventory,
			RunState.PlayerFleetUnitId);
		foreach (var other in run.PilotRegistry.Assigned.Where(active => active.Id != pilot.Id).ToArray())
			run.PilotRegistry.Unassign(other.Id);
		return run.PilotRegistry.Assigned.Single();
	}

	private static void AddReservePilots(RunState run)
	{
		for (var index = 0; index < 4; index++)
			run.PilotRegistry.Register(PilotWith(0, $"bench-{index}"));
	}

	private static void StartJourney(RunState run)
	{
		var destination = run.StarSystem.Map.DocksByPoiId[SupplySystemPlan.Copper.StoragePoiId].Position;
		run.StarSystem.PlayerAgent!.TryQueueMove(destination);
		run.StarSystem.AdvanceTick();
	}

	private static RollForDistressCallAction TriggeringRoll(RunState run, string? responderId = null)
	{
		var pilots = WorldEventContexts.RosterPilots(run.StarSystem.Map).ToArray();
		for (ulong seed = 0; seed < 10_000; seed++)
		{
			if (RollForDistressCallDef.SelectResponder(seed, pilots, run.PilotRegistry.CanRegister) is { } responder
				&& (responderId is null || responder.Id == responderId))
				return new RollForDistressCallAction(StarSystemActorIds.WorldEvents, seed);
		}

		throw new InvalidOperationException("No seed triggered a distress call.");
	}

	private static Pilot? Responder(RunState run, ulong seed) =>
		RollForDistressCallDef.SelectResponder(
			seed,
			WorldEventContexts.RosterPilots(run.StarSystem.Map),
			run.PilotRegistry.CanRegister);

	private static PendingWorldEvent OpenLoot(RunState run, ResourceBundle reward)
	{
		var pilot = run.PilotRegistry.Assigned[0];
		var pending = new PendingWorldEvent(
			"world-event-distress-loot",
			EWorldEventId.DistressCall,
			pilot.Id,
			pilot.AssignedShipId!,
			0,
			WorldEventCatalog.Distress.AnnounceKill(pilot.Name),
			reward);
		run.StarSystem.CommitSetup(new DistressCallReceivedAction(StarSystemActorIds.WorldEvents, pending));
		return pending;
	}

	private static PendingWorldEvent OpenRecruit(RunState run)
	{
		var pilot = run.PilotRegistry.Assigned[0];
		var recruit = RollForDistressCallDef.RollRecruit(3);
		var pending = new PendingWorldEvent(
			"world-event-distress-recruit",
			EWorldEventId.DistressCall,
			pilot.Id,
			pilot.AssignedShipId!,
			0,
			WorldEventCatalog.Distress.AnnounceSurvivor(pilot.Name, recruit.Name),
			Recruit: recruit);
		run.StarSystem.CommitSetup(new DistressCallReceivedAction(StarSystemActorIds.WorldEvents, pending));
		return pending;
	}

	private static AcknowledgeDistressCallAction Acknowledge(PendingWorldEvent pending) =>
		new(RunState.PlayerFleetUnitId, pending.Id);

	private static AcceptDistressRecruitAction Accept(PendingWorldEvent pending) =>
		new(RunState.PlayerFleetUnitId, pending.Id);

	private static RejectDistressRecruitAction Reject(PendingWorldEvent pending) =>
		new(RunState.PlayerFleetUnitId, pending.Id);

	private static int Balance(RunState run, ResourceId id) =>
		run.StarSystem.Map.PlayerResources.GetBalance(id);

	private static int Amount(ResourceBundle bundle, ResourceId id) =>
		bundle.TryGet(id, out var amount) ? amount : 0;
}
