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
public sealed class DebrisDetourTests
{
	[Theory]
	[InlineData(-5, 0.20)]
	[InlineData(0, 0)]
	[InlineData(5, 0)]
	public void TriggerChance_RisesWithCaution(int threatResponse, double expected) =>
		Assert.Equal(
			expected,
			RollForDebrisDetourDef.TriggerChance(PilotWith(threatResponse)),
			precision: 10);

	[Theory]
	[InlineData(-5, 0.18, 0.22)]
	[InlineData(5, 0, 0)]
	public void SelectNavigator_TriggersAtPersonalityAdjustedRate(
		int threatResponse,
		double minRate,
		double maxRate)
	{
		var pilots = new[] { PilotWith(threatResponse) };

		var triggers = Enumerable.Range(0, 5_000)
			.Count(seed => RollForDebrisDetourDef.SelectNavigator((ulong)seed, pilots) is not null);

		Assert.InRange(triggers / 5_000.0, minRate, maxRate);
	}

	[Fact]
	public void Roll_WhileTraveling_OpensTheDetour()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var navigator = FieldOnly(run, threatResponse: -5);
		StartJourney(run);
		var roll = TriggeringRoll(run);

		run.StarSystem.CommitSetup(roll);
		run.StarSystem.AdvanceTick();

		var pending = run.StarSystem.Map.PendingWorldEvent;
		Assert.NotNull(pending);
		Assert.Equal(EWorldEventId.DebrisDetour, pending.EventId);
		Assert.Equal(navigator.Id, pending.PilotId);
		Assert.Equal(
			ResourceBundle.Of(ResourceId.Fuel, -WorldEventCatalog.DebrisDetour.FuelCost),
			pending.Reward);
		Assert.Equal(WorldEventCatalog.DebrisDetour.Announce(navigator.Name), pending.Presentation);
		Assert.True(run.StarSystem.Map.WaitingForPlayerInput);
	}

	[Fact]
	public void Roll_WhileDocked_PassesSilently()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		FieldOnly(run, threatResponse: -5);

		run.StarSystem.CommitSetup(TriggeringRoll(run));
		run.StarSystem.AdvanceTick();

		Assert.Null(run.StarSystem.Map.PendingWorldEvent);
	}

	[Fact]
	public void Roll_ForAnAggressivePilot_PassesSilently()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		FieldOnly(run, threatResponse: 5);
		StartJourney(run);

		run.StarSystem.CommitSetup(new RollForDebrisDetourAction(StarSystemActorIds.WorldEvents, 1));
		run.StarSystem.AdvanceTick();

		Assert.Null(run.StarSystem.Map.PendingWorldEvent);
	}

	[Fact]
	public void Roll_WithoutEnoughFuel_PassesSilently()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		FieldOnly(run, threatResponse: -5);
		StartJourney(run);
		var surplus = FuelOf(run) - (WorldEventCatalog.DebrisDetour.FuelCost - 1);
		Assert.True(run.StarSystem.Map.PlayerResources.TryApply(
			ResourceBundle.Of(ResourceId.Fuel, -surplus)));

		run.StarSystem.CommitSetup(TriggeringRoll(run));
		run.StarSystem.AdvanceTick();

		Assert.Null(run.StarSystem.Map.PendingWorldEvent);
		Assert.Equal(WorldEventCatalog.DebrisDetour.FuelCost - 1, FuelOf(run));
	}

	[Fact]
	public void Acknowledge_SpendsFuelOnce()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pending = OpenDetour(run);
		var fuel = FuelOf(run);

		run.StarSystem.CommitSetup(Acknowledge(pending));
		run.StarSystem.CommitSetup(Acknowledge(pending));

		Assert.Equal(fuel - WorldEventCatalog.DebrisDetour.FuelCost, FuelOf(run));
		Assert.Null(run.StarSystem.Map.PendingWorldEvent);
		Assert.False(run.StarSystem.Map.WaitingForPlayerInput);
	}

	[Fact]
	public void SaveLoad_StillSpendsFuelOnce()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pending = OpenDetour(run);
		var registry = PersistenceRegistry.CreateDefault();

		var snapshot = run.CaptureSnapshot(registry);
		using var restored = RunState.FromSnapshot(snapshot, registry);
		var fuel = FuelOf(restored);

		restored.StarSystem.CommitSetup(Acknowledge(pending));
		restored.StarSystem.CommitSetup(Acknowledge(pending));

		Assert.Equal(fuel - WorldEventCatalog.DebrisDetour.FuelCost, FuelOf(restored));
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

	private static RollForDebrisDetourAction TriggeringRoll(RunState run)
	{
		var pilots = WorldEventContexts.RosterPilots(run.StarSystem.Map).ToArray();
		for (ulong seed = 0; seed < 10_000; seed++)
		{
			if (RollForDebrisDetourDef.SelectNavigator(seed, pilots) is not null)
				return new RollForDebrisDetourAction(StarSystemActorIds.WorldEvents, seed);
		}

		throw new InvalidOperationException("No seed triggered a debris detour.");
	}

	private static PendingWorldEvent OpenDetour(RunState run)
	{
		var pilot = run.PilotRegistry.Assigned[0];
		var pending = new PendingWorldEvent(
			"world-event-detour",
			EWorldEventId.DebrisDetour,
			pilot.Id,
			pilot.AssignedShipId!,
			0,
			WorldEventCatalog.DebrisDetour.Announce(pilot.Name),
			ResourceBundle.Of(ResourceId.Fuel, -WorldEventCatalog.DebrisDetour.FuelCost));
		run.StarSystem.CommitSetup(new DebrisDetourReportedAction(StarSystemActorIds.WorldEvents, pending));
		return pending;
	}

	private static AcknowledgeDebrisDetourAction Acknowledge(PendingWorldEvent pending) =>
		new(RunState.PlayerFleetUnitId, pending.Id);

	private static int FuelOf(RunState run) =>
		run.StarSystem.Map.PlayerResources.GetBalance(ResourceId.Fuel);
}
