using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.Tests.World.StarSystem.Traffic;

public sealed class TrafficSimulationTests
{
	[Fact]
	public void AdvanceTick_DepartsDockedUnitAndRegistersLane()
	{
		var orchestrator = StarSystemOrchestrator.FromMap(StarMap.CreateDevDefault(42));
		var map = orchestrator.Map;
		var cargo = map.UnitRegistry.UnitOf(Factory.CargoShuttleId).State;

		orchestrator.AdvanceTick();

		Assert.Equal(EPhase.InTransit, cargo.Phase);
		Assert.NotNull(cargo.Journey.RouteId);
		Assert.Contains(Factory.CargoShuttleId, map.TrafficController.OccupantsByRouteId[cargo.Journey.RouteId!]);

		orchestrator.AdvanceTick();

		Assert.Equal(EPhase.InTransit, cargo.Phase);
		Assert.True(cargo.Journey.LongitudinalProgress > 0);
	}

	[Fact]
	public void AdvanceTick_AdvancesJourneyProgressWhileInTransit()
	{
		var orchestrator = StarSystemOrchestrator.FromMap(StarMap.CreateDevDefault(42));
		var cargo = orchestrator.Map.UnitRegistry.UnitOf(Factory.CargoShuttleId).State;

		orchestrator.AdvanceTicks(2);
		var progressAfterDepart = cargo.Journey.LongitudinalProgress;

		orchestrator.AdvanceTick();

		Assert.Equal(EPhase.InTransit, cargo.Phase);
		Assert.True(cargo.Journey.LongitudinalProgress > progressAfterDepart);
	}

	[Fact]
	public void AdvanceTicks_CompletesCargoChoreLoop()
	{
		var orchestrator = StarSystemOrchestrator.FromMap(StarMap.CreateDevDefault(42));
		var map = orchestrator.Map;
		var stationDock = map.DocksByPoiId["station-dev"].Id;
		var planetADock = map.DocksByPoiId["planet-dev-a"].Id;
		var cargo = map.UnitRegistry.UnitOf(Factory.CargoShuttleId).State;
		var visited = new HashSet<string>(StringComparer.Ordinal);

		for (var tick = 0; tick < 1000; tick++)
		{
			orchestrator.AdvanceTick();
			if (cargo.Phase == EPhase.Working)
				visited.Add(cargo.DockedAtDockId);
		}

		Assert.Contains(planetADock, visited);
		Assert.Contains(stationDock, visited);
	}

	[Fact]
	public void AdvanceTicks_PatrolVisitsBothPlanets()
	{
		var orchestrator = StarSystemOrchestrator.FromMap(StarMap.CreateDevDefault(42));
		var map = orchestrator.Map;
		var patrol = map.UnitRegistry.UnitOf(Factory.PatrolId).State;
		var planetADock = map.DocksByPoiId["planet-dev-a"].Id;
		var planetBDock = map.DocksByPoiId["planet-dev-b"].Id;
		var visited = new HashSet<string>(StringComparer.Ordinal);

		for (var tick = 0; tick < 1200; tick++)
		{
			orchestrator.AdvanceTick();
			if (patrol.Phase == EPhase.Working)
				visited.Add(patrol.DockedAtDockId);
		}

		Assert.Contains(planetADock, visited);
		Assert.Contains(planetBDock, visited);
	}

	[Fact]
	public void AdvanceTicks_200TickLoop_DoesNotThrowAndKeepsControllerValid()
	{
		var orchestrator = StarSystemOrchestrator.FromMap(StarMap.CreateDevDefault(7));

		orchestrator.AdvanceTicks(200);

		orchestrator.Map.TrafficController.Validate();
		Assert.Equal(201, orchestrator.Tick);
	}

	[Fact]
	public void Fork_KeepsTrafficStateIndependent()
	{
		var orchestrator = StarSystemOrchestrator.FromMap(StarMap.CreateDevDefault(11));
		orchestrator.AdvanceTicks(25);

		var forkedMap = orchestrator.Map.Fork();
		var forkedOrchestrator = StarSystemOrchestrator.FromMap(forkedMap);
		forkedOrchestrator.AdvanceTicks(10);

		Assert.NotSame(orchestrator.Map.Timeline, forkedOrchestrator.Map.Timeline);
		Assert.NotSame(orchestrator.Map.TrafficController, forkedOrchestrator.Map.TrafficController);
		Assert.NotSame(orchestrator.Map.UnitRegistry, forkedOrchestrator.Map.UnitRegistry);
		Assert.Same(orchestrator.Map.RoutesById, forkedOrchestrator.Map.RoutesById);

		var originalCargo = orchestrator.Map.UnitRegistry.UnitOf(Factory.CargoShuttleId).State;
		var forkedCargo = forkedOrchestrator.Map.UnitRegistry.UnitOf(Factory.CargoShuttleId).State;
		Assert.NotEqual(
			originalCargo.Journey.LongitudinalProgress,
			forkedCargo.Journey.LongitudinalProgress);
	}
}
