using GrimSpace.Tests.Run;
using GrimSpace.Tests.World.StarSystem.Traffic;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Economy;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Units;
using RunState = GrimSpace.Run.State;

namespace GrimSpace.Tests.World.StarSystem.Economy;

[StarSystemTestSuite]
public sealed class TravelFuelEconomyTests(StarMapFixture maps)
{
	[Fact]
	public void AdvanceTick_WhileJourneying_BurnsFuelIncrementally()
	{
		var orchestrator = CreatePlayerOrchestrator();
		var destination = orchestrator.Map.DocksByPoiId[SupplySystemPlan.Copper.StoragePoiId].Position;
		var startFuel = orchestrator.Map.PlayerResources.GetBalance(ResourceId.Fuel);

		orchestrator.PlayerAgent!.TryQueueMove(destination);
		orchestrator.AdvanceTick();
		Assert.IsType<FleetTravel.Journey>(
			orchestrator.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId).State.Travel);

		orchestrator.AdvanceTicks(5);

		var fuel = orchestrator.Map.PlayerResources.GetBalance(ResourceId.Fuel);
		Assert.True(fuel < startFuel, $"expected fuel burn, start={startFuel} now={fuel}");
		var runtime = orchestrator.RuntimeFor(RunState.PlayerFleetUnitId);
		Assert.True(runtime.FuelChargedDistanceAlongCurrentJourney > 0);
		Assert.Equal(
			orchestrator.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId).State.Journey().Id,
			runtime.FuelLedgerJourneyId);
	}

	[Fact]
	public void Repath_PreservesUnchargedRemainder()
	{
		var orchestrator = CreatePlayerOrchestrator();
		var first = orchestrator.Map.DocksByPoiId[SupplySystemPlan.Copper.StoragePoiId].Position;
		var second = orchestrator.Map.DocksByPoiId[SupplySystemPlan.Copper.ExitPoiId].Position;
		var runtime = orchestrator.RuntimeFor(RunState.PlayerFleetUnitId);
		var player = orchestrator.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);

		orchestrator.PlayerAgent!.TryQueueMove(first);
		orchestrator.AdvanceTick();
		var firstJourneyId = player.State.Journey().Id;
		runtime.FuelUnchargedDistance = 7;
		runtime.FuelChargedDistanceAlongCurrentJourney = 3;
		runtime.FuelLedgerJourneyId = firstJourneyId;
		var fuelBeforeRepath = orchestrator.Map.PlayerResources.GetBalance(ResourceId.Fuel);

		orchestrator.PlayerAgent!.TryQueueMove(second);
		orchestrator.AdvanceTick();

		Assert.NotEqual(firstJourneyId, player.State.Journey().Id);
		Assert.Equal(player.State.Journey().Id, runtime.FuelLedgerJourneyId);
		var fuelBurned = fuelBeforeRepath - orchestrator.Map.PlayerResources.GetBalance(ResourceId.Fuel);
		var remainderPlusBurnedDistance =
			runtime.FuelUnchargedDistance + fuelBurned * StrategicEconomyConfig.DistancePerFuelUnit;
		Assert.True(
			remainderPlusBurnedDistance >= 7,
			$"expected preserved remainder accounting, got uncharged={runtime.FuelUnchargedDistance}, burned={fuelBurned}");
	}

	[Fact]
	public void Stop_ClearsTravelFuelLedger()
	{
		var orchestrator = CreatePlayerOrchestrator();
		var destination = orchestrator.Map.DocksByPoiId[SupplySystemPlan.Copper.StoragePoiId].Position;
		var runtime = orchestrator.RuntimeFor(RunState.PlayerFleetUnitId);
		var player = orchestrator.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);

		orchestrator.PlayerAgent!.TryQueueMove(destination);
		orchestrator.AdvanceTick();
		var duration = runtime.CachedPath!.DurationTicks(player.State.SpeedPerTick);
		orchestrator.AdvanceTicks(duration + 1);

		Assert.IsType<FleetTravel.AtRest>(player.State.Travel);
		Assert.Equal(0, runtime.FuelLedgerJourneyId);
		Assert.Equal(0, runtime.FuelChargedDistanceAlongCurrentJourney);
		Assert.Equal(0, runtime.FuelUnchargedDistance);
	}

	[Fact]
	public void SaveLoad_MidRoute_PreservesFuelLedger()
	{
		using var run = RunState.CreateNewRun(42);
		var orchestrator = run.StarSystem;
		var destination = orchestrator.Map.DocksByPoiId[SupplySystemPlan.Copper.StoragePoiId].Position;
		orchestrator.PlayerAgent!.TryQueueMove(destination);
		orchestrator.AdvanceTick();
		orchestrator.AdvanceTicks(3);

		var runtime = orchestrator.RuntimeFor(RunState.PlayerFleetUnitId);
		Assert.True(runtime.FuelLedgerJourneyId != 0);

		var registry = GrimSpace.Run.Persistence.PersistenceRegistry.CreateDefault();
		var snapshot = run.CaptureSnapshot(registry);
		using var restored = RunState.FromSnapshot(snapshot, registry);
		var restoredRuntime = restored.StarSystem.RuntimeFor(RunState.PlayerFleetUnitId);

		Assert.Equal(runtime.FuelLedgerJourneyId, restoredRuntime.FuelLedgerJourneyId);
		Assert.Equal(
			runtime.FuelChargedDistanceAlongCurrentJourney,
			restoredRuntime.FuelChargedDistanceAlongCurrentJourney);
		Assert.Equal(runtime.FuelUnchargedDistance, restoredRuntime.FuelUnchargedDistance);
		Assert.Equal(
			orchestrator.Map.PlayerResources.GetBalance(ResourceId.Fuel),
			restored.StarSystem.Map.PlayerResources.GetBalance(ResourceId.Fuel));
	}

	[Fact]
	public void Starvation_DamagesFieldedHullWhenFuelIsEmpty()
	{
		using var run = RunState.CreateNewRun(42);
		var fuel = run.StarSystem.Map.PlayerResources.GetBalance(ResourceId.Fuel);
		Assert.True(
			run.StarSystem.Map.PlayerResources.TryApply(
				ResourceBundle.Of(ResourceId.Fuel, -fuel)));

		var shipId = RunTestKit.StarterFighterShipId(run);
		var hullBefore = run.ItemInventory.Get(shipId).HullPoints;
		var destination = run.StarSystem.Map.DocksByPoiId[SupplySystemPlan.Copper.StoragePoiId].Position;

		// CreateNewRun leaves WaitingForPlayerInput; queueing a move advances the clock once.
		Assert.IsType<CourseCommandResult.Queued>(
			run.StarSystem.PlayerAgent!.TryQueueMove(destination));

		Assert.IsType<FleetTravel.Journey>(
			run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId).State.Travel);
		Assert.Equal(
			hullBefore - StrategicEconomyConfig.StarvationHullDamagePerTick,
			run.ItemInventory.Get(shipId).HullPoints);
	}

	private StarSystemOrchestrator CreatePlayerOrchestrator() =>
		StarSystemTestHarness.CreatePlayerOrchestrator(maps, RunState.PlayerFleetUnitId, seed: 42);
}
