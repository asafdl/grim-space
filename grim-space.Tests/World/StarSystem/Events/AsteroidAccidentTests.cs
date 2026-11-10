using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Run.Persistence;
using GrimSpace.Tests.Run;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Events;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Ids;
using RunState = GrimSpace.Run.State;

namespace GrimSpace.Tests.World.StarSystem.Events;

[StarSystemTestSuite]
public sealed class AsteroidAccidentTests
{
	[Theory]
	[InlineData(-5, 0.20)]
	[InlineData(0, 0)]
	[InlineData(5, 0)]
	public void TriggerChance_RisesWithImpulsiveness(int deliberation, double expected) =>
		Assert.Equal(
			expected,
			RollForAsteroidDodgeDef.TriggerChance(PilotWith(deliberation)),
			precision: 10);

	[Theory]
	[InlineData(-5, 0.18, 0.22)]
	[InlineData(5, 0, 0)]
	public void SelectCulprit_TriggersAtPersonalityAdjustedRate(
		int deliberation,
		double minRate,
		double maxRate)
	{
		var pilots = new[] { PilotWith(deliberation) };

		var triggers = Enumerable.Range(0, 5_000)
			.Count(seed => RollForAsteroidDodgeDef.SelectCulprit((ulong)seed, pilots) is not null);

		Assert.InRange(triggers / 5_000.0, minRate, maxRate);
	}

	[Fact]
	public void SelectCulprit_IsDeterministicForSeed()
	{
		var pilots = new[] { PilotWith(-5, "a"), PilotWith(-5, "b"), PilotWith(-5, "c") };

		var results = Enumerable.Range(0, 200)
			.Select(seed => RollForAsteroidDodgeDef.SelectCulprit((ulong)seed, pilots)?.Name)
			.ToArray();
		var repeated = Enumerable.Range(0, 200)
			.Select(seed => RollForAsteroidDodgeDef.SelectCulprit((ulong)seed, pilots)?.Name)
			.ToArray();

		Assert.Equal(results, repeated);
		Assert.Contains("a", results);
		Assert.Contains("b", results);
		Assert.Contains("c", results);
	}

	[Fact]
	public void SelectCulprit_WithoutPilots_ReturnsNull() =>
		Assert.Null(RollForAsteroidDodgeDef.SelectCulprit(1, Array.Empty<Pilot>()));

	[Fact]
	public void Roll_WhileTraveling_OpensAnnouncementOnNextTick()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		StartJourney(run);
		var roll = TriggeringRoll(run);

		run.StarSystem.CommitSetup(roll);
		Assert.True(run.StarSystem.Map.Timeline.ContainsPending(action => action is AsteroidImpactAction));
		Assert.Null(run.StarSystem.Map.PendingWorldEvent);

		run.StarSystem.AdvanceTick();

		var pending = run.StarSystem.Map.PendingWorldEvent;
		Assert.NotNull(pending);
		Assert.Equal(EWorldEventId.AsteroidAccident, pending.EventId);
		Assert.True(run.StarSystem.Map.WaitingForPlayerInput);
		Assert.Contains(pending.PilotId, run.PilotRegistry.Assigned.Select(pilot => pilot.Id));
	}

	[Fact]
	public void Roll_WhileDocked_PassesSilently()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var roll = TriggeringRoll(run);

		run.StarSystem.CommitSetup(roll);
		run.StarSystem.AdvanceTick();

		Assert.False(run.StarSystem.Map.Timeline.ContainsPending(action => action is AsteroidImpactAction));
		Assert.Null(run.StarSystem.Map.PendingWorldEvent);
		Assert.False(run.StarSystem.Map.WaitingForPlayerInput);
	}

	[Fact]
	public void Roll_WhileAnotherEventIsPending_PassesSilently()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		StartJourney(run);
		OpenAnnouncement(run);

		run.StarSystem.CommitSetup(TriggeringRoll(run));

		Assert.False(run.StarSystem.Map.Timeline.ContainsPending(action => action is AsteroidImpactAction));
	}

	[Fact]
	public void Roll_AnnouncesDamageActuallyApplied()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		StartJourney(run);
		var roll = TriggeringRoll(run);
		var culprit = RollForAsteroidDodgeDef.SelectCulprit(
			roll.RngSeed,
			WorldEventContexts.FieldedPilots(run.StarSystem.Map))!;
		ClearShields(run, culprit.AssignedShipId!);
		SetHull(run, culprit.AssignedShipId!, 5);

		run.StarSystem.CommitSetup(roll);

		var impact = run.StarSystem.Map.Timeline.ToSnapshot().Pending.Values
			.SelectMany(actions => actions.OfType<AsteroidImpactAction>())
			.Single();
		Assert.Equal(
			WorldEventCatalog.Asteroid.Announce(culprit.Name, 0, 4),
			impact.Pending.Presentation);
		Assert.Contains("4 hull damage", impact.Pending.Presentation.Body);
		Assert.Contains(culprit.Name, impact.Pending.Presentation.Body);
	}

	[Fact]
	public void Acknowledge_AppliesDamageOnceAndReleasesTheWait()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pending = OpenAnnouncement(run);
		ClearShields(run, pending.ShipId);
		var before = HullOf(run, pending.ShipId);

		run.StarSystem.CommitSetup(Acknowledge(pending));

		Assert.Equal(before - pending.Damage, HullOf(run, pending.ShipId));
		Assert.Null(run.StarSystem.Map.PendingWorldEvent);
		Assert.False(run.StarSystem.Map.WaitingForPlayerInput);

		run.StarSystem.CommitSetup(Acknowledge(pending));

		Assert.Equal(before - pending.Damage, HullOf(run, pending.ShipId));
	}

	[Fact]
	public void Acknowledge_BeforeAnnouncementOrWithWrongId_DoesNothing()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pending = OpenAnnouncement(run);
		var before = HullOf(run, pending.ShipId);

		run.StarSystem.CommitSetup(new AcknowledgeAsteroidImpactAction(
			RunState.PlayerFleetUnitId,
			"world-event-stale"));

		Assert.Equal(before, HullOf(run, pending.ShipId));
		Assert.Equal(pending, run.StarSystem.Map.PendingWorldEvent);
		Assert.True(run.StarSystem.Map.WaitingForPlayerInput);
	}

	[Fact]
	public void Acknowledge_NeverDestroysTheShip()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pending = OpenAnnouncement(run);
		ClearShields(run, pending.ShipId);
		SetHull(run, pending.ShipId, 5);

		run.StarSystem.CommitSetup(Acknowledge(pending));

		Assert.Equal(1, HullOf(run, pending.ShipId));
	}

	[Fact]
	public void Acknowledge_SpendsShieldsBeforeHull()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pending = OpenAnnouncement(run);
		var ship = run.ItemInventory.Get(pending.ShipId).Clone();
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			ship.CurrentShieldPoints[face] = 3;
		ship.CurrentHullPoints = 20;
		run.ItemInventory.Update(ship);

		run.StarSystem.CommitSetup(Acknowledge(pending));

		var after = run.ItemInventory.Get(pending.ShipId);
		Assert.Equal(20, after.CurrentHullPoints);
		Assert.Equal(18 - pending.Damage, after.TotalCurrentShieldPoints);
	}

	[Fact]
	public void Acknowledge_DoesNotDamageShipThatIsNoLongerFielded()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pending = OpenAnnouncement(run);
		run.PilotRegistry.Unassign(run.PilotRegistry.Assigned[0].Id);
		var before = HullOf(run, pending.ShipId);

		run.StarSystem.CommitSetup(Acknowledge(pending));

		Assert.Equal(before, HullOf(run, pending.ShipId));
		Assert.Null(run.StarSystem.Map.PendingWorldEvent);
		Assert.False(run.StarSystem.Map.WaitingForPlayerInput);
	}

	[Fact]
	public void Fork_PreservesPendingEvent()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pending = OpenAnnouncement(run);

		var fork = run.StarSystem.Map.ForkForSimulation();

		Assert.Equal(pending, fork.PendingWorldEvent);
		Assert.True(fork.WaitingForPlayerInput);
	}

	[Fact]
	public void SaveLoad_PreservesPendingEventAndStillAcknowledgesOnce()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pending = OpenAnnouncement(run);
		ClearShields(run, pending.ShipId);
		var registry = PersistenceRegistry.CreateDefault();

		var snapshot = run.CaptureSnapshot(registry);
		using var restored = RunState.FromSnapshot(snapshot, registry);

		Assert.Equal(pending, restored.StarSystem.Map.PendingWorldEvent);
		Assert.True(restored.StarSystem.Map.WaitingForPlayerInput);
		var before = HullOf(restored, pending.ShipId);

		restored.StarSystem.CommitSetup(Acknowledge(pending));
		restored.StarSystem.CommitSetup(Acknowledge(pending));

		Assert.Equal(before - pending.Damage, HullOf(restored, pending.ShipId));
		Assert.Null(restored.StarSystem.Map.PendingWorldEvent);
	}

	private static Pilot PilotWith(int deliberation, string name = "pilot") =>
		Pilot.FromRecruitProfile(
			null,
			new PilotRecruitProfile(
				name,
				"pilot_01",
				EPilotBackgroundId.HydraulicAbuser, // zero deliberation modifier
				personality: new PilotPersonality(0, deliberation, 0)));

	private static void StartJourney(RunState run)
	{
		var destination = run.StarSystem.Map.DocksByPoiId[SupplySystemPlan.Copper.StoragePoiId].Position;
		run.StarSystem.PlayerAgent!.TryQueueMove(destination);
		run.StarSystem.AdvanceTick();
	}

	private static RollForAsteroidDodgeAction TriggeringRoll(RunState run)
	{
		var pilots = WorldEventContexts.FieldedPilots(run.StarSystem.Map).ToArray();
		for (ulong seed = 0; seed < 10_000; seed++)
		{
			if (RollForAsteroidDodgeDef.SelectCulprit(seed, pilots) is not null)
				return new RollForAsteroidDodgeAction(StarSystemActorIds.WorldEvents, seed);
		}

		throw new InvalidOperationException("No seed triggered an asteroid accident.");
	}

	private static PendingWorldEvent OpenAnnouncement(RunState run)
	{
		var pilot = run.PilotRegistry.Assigned[0];
		var pending = new PendingWorldEvent(
			"world-event-test",
			EWorldEventId.AsteroidAccident,
			pilot.Id,
			pilot.AssignedShipId!,
			WorldEventCatalog.Asteroid.Damage,
			new WorldEventPresentation("Asteroid Strike", "Test body."));
		run.StarSystem.CommitSetup(new AsteroidImpactAction(StarSystemActorIds.WorldEvents, pending));
		Assert.Equal(pending, run.StarSystem.Map.PendingWorldEvent);
		return pending;
	}

	private static AcknowledgeAsteroidImpactAction Acknowledge(PendingWorldEvent pending) =>
		new(RunState.PlayerFleetUnitId, pending.Id);

	private static int HullOf(RunState run, string shipId) =>
		run.ItemInventory.Get(shipId).CurrentHullPoints;

	private static void ClearShields(RunState run, string shipId)
	{
		var ship = run.ItemInventory.Get(shipId).Clone();
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			ship.CurrentShieldPoints[face] = 0;
		run.ItemInventory.Update(ship);
	}

	private static void SetHull(RunState run, string shipId, int hull)
	{
		var ship = run.ItemInventory.Get(shipId).Clone();
		ship.CurrentHullPoints = hull;
		run.ItemInventory.Update(ship);
	}
}
