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
public sealed class PotshotTests
{
	[Theory]
	[InlineData(5, 0.20)]
	[InlineData(0, 0)]
	[InlineData(-5, 0)]
	public void TriggerChance_RisesWithAggression(int threatResponse, double expected) =>
		Assert.Equal(
			expected,
			RollForPotshotDef.TriggerChance(PilotWith(threatResponse)),
			precision: 10);

	[Theory]
	[InlineData(5, 0.18, 0.22)]
	[InlineData(-5, 0, 0)]
	public void SelectShooter_TriggersAtPersonalityAdjustedRate(
		int threatResponse,
		double minRate,
		double maxRate)
	{
		var pilots = new[] { PilotWith(threatResponse) };

		var triggers = Enumerable.Range(0, 5_000)
			.Count(seed => RollForPotshotDef.SelectShooter((ulong)seed, pilots) is not null);

		Assert.InRange(triggers / 5_000.0, minRate, maxRate);
	}

	[Fact]
	public void Roll_WhileTraveling_OpensTheCruiserReturnFire()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var shooter = FieldOnly(run, threatResponse: 5);
		StartJourney(run);
		ClearShields(run, shooter.AssignedShipId!);
		var hull = run.ItemInventory.Get(shooter.AssignedShipId!).CurrentHullPoints;
		var hullLost = System.Math.Min(WorldEventCatalog.Potshot.Damage, hull - 1);
		var roll = TriggeringRoll(run);

		run.StarSystem.CommitSetup(roll);
		run.StarSystem.AdvanceTick();

		var pending = run.StarSystem.Map.PendingWorldEvent;
		Assert.NotNull(pending);
		Assert.Equal(EWorldEventId.Potshot, pending.EventId);
		Assert.Equal(shooter.Id, pending.PilotId);
		Assert.Equal(WorldEventCatalog.Potshot.Damage, pending.Damage);
		Assert.Equal(
			WorldEventCatalog.Potshot.Announce(shooter.Name, 0, hullLost),
			pending.Presentation);
		Assert.True(run.StarSystem.Map.WaitingForPlayerInput);
	}

	[Fact]
	public void Roll_WhileDocked_PassesSilently()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		FieldOnly(run, threatResponse: 5);

		run.StarSystem.CommitSetup(TriggeringRoll(run));
		run.StarSystem.AdvanceTick();

		Assert.Null(run.StarSystem.Map.PendingWorldEvent);
		Assert.False(run.StarSystem.Map.WaitingForPlayerInput);
	}

	[Fact]
	public void Roll_ForAFearfulPilot_PassesSilently()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		FieldOnly(run, threatResponse: -5);
		StartJourney(run);

		run.StarSystem.CommitSetup(new RollForPotshotAction(StarSystemActorIds.WorldEvents, 1));
		run.StarSystem.AdvanceTick();

		Assert.Null(run.StarSystem.Map.PendingWorldEvent);
	}

	[Fact]
	public void Acknowledge_AppliesDamageOnceAndCannotDestroyTheShip()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pending = OpenPotshot(run);
		ClearShields(run, pending.ShipId);
		SetHull(run, pending.ShipId, 5);

		run.StarSystem.CommitSetup(Acknowledge(pending));
		run.StarSystem.CommitSetup(Acknowledge(pending));

		Assert.Equal(1, HullOf(run, pending.ShipId));
		Assert.Null(run.StarSystem.Map.PendingWorldEvent);
		Assert.False(run.StarSystem.Map.WaitingForPlayerInput);
	}

	[Fact]
	public void SaveLoad_StillAppliesDamageOnce()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pending = OpenPotshot(run);
		ClearShields(run, pending.ShipId);
		var registry = PersistenceRegistry.CreateDefault();

		var snapshot = run.CaptureSnapshot(registry);
		using var restored = RunState.FromSnapshot(snapshot, registry);
		var before = HullOf(restored, pending.ShipId);

		restored.StarSystem.CommitSetup(Acknowledge(pending));
		restored.StarSystem.CommitSetup(Acknowledge(pending));

		Assert.Equal(before - pending.Damage, HullOf(restored, pending.ShipId));
		Assert.Null(restored.StarSystem.Map.PendingWorldEvent);
	}

	private static Pilot PilotWith(int threatResponse, string name = "pilot") =>
		Pilot.FromRecruitProfile(
			null,
			new PilotRecruitProfile(
				name,
				"pilot_01",
				EPilotBackgroundId.PodRacer,
				personality: new PilotPersonality(threatResponse, 0, 0)));

	private static Pilot FieldOnly(RunState run, int threatResponse)
	{
		var ranger = RunTestKit.StarterRangerPilot(run);
		var shipId = ranger.AssignedShipId!;
		run.PilotRegistry.Unassign(ranger.Id);
		var pilot = PilotWith(threatResponse, "Keel");
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

	private static void StartJourney(RunState run)
	{
		var destination = run.StarSystem.Map.DocksByPoiId[SupplySystemPlan.Copper.StoragePoiId].Position;
		run.StarSystem.PlayerAgent!.TryQueueMove(destination);
		run.StarSystem.AdvanceTick();
	}

	private static RollForPotshotAction TriggeringRoll(RunState run)
	{
		var pilots = WorldEventContexts.FieldedPilots(run.StarSystem.Map).ToArray();
		for (ulong seed = 0; seed < 10_000; seed++)
		{
			if (RollForPotshotDef.SelectShooter(seed, pilots) is not null)
				return new RollForPotshotAction(StarSystemActorIds.WorldEvents, seed);
		}

		throw new InvalidOperationException("No seed triggered a potshot.");
	}

	private static PendingWorldEvent OpenPotshot(RunState run)
	{
		var pilot = run.PilotRegistry.Assigned[0];
		var pending = new PendingWorldEvent(
			"world-event-potshot",
			EWorldEventId.Potshot,
			pilot.Id,
			pilot.AssignedShipId!,
			WorldEventCatalog.Potshot.Damage,
			WorldEventCatalog.Potshot.Announce(pilot.Name, 0, WorldEventCatalog.Potshot.Damage));
		run.StarSystem.CommitSetup(new PotshotReportedAction(StarSystemActorIds.WorldEvents, pending));
		return pending;
	}

	private static AcknowledgePotshotAction Acknowledge(PendingWorldEvent pending) =>
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
