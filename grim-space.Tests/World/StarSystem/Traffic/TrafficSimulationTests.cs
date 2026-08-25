using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Traffic;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.Tests.World.StarSystem.Traffic;

public sealed class TrafficSimulationTests
{
	[Fact]
	public void AdvanceTick_DepartsDockedUnitAndRegistersLane()
	{
		var orchestrator = StarSystemOrchestrator.FromMap(StarMap.CreateDevDefault(42));
		var map = orchestrator.Map;
		var readyUnit = map.UnitRegistry.All.First(unit => unit.State.IsReadyToDepart).State;

		orchestrator.AdvanceTick();

		Assert.Equal(EPhase.InTransit, readyUnit.Phase);
		Assert.NotNull(readyUnit.Journey.RouteId);
		Assert.Contains(
			readyUnit.Id,
			map.TrafficController.OccupantsByRouteId[readyUnit.Journey.RouteId!]);

		orchestrator.AdvanceTick();

		Assert.Equal(EPhase.InTransit, readyUnit.Phase);
		Assert.True(readyUnit.Journey.LongitudinalProgress > 0);
	}

	[Fact]
	public void AdvanceTick_AdvancesJourneyProgressWhileInTransit()
	{
		var orchestrator = StarSystemOrchestrator.FromMap(StarMap.CreateDevDefault(42));
		var unit = orchestrator.Map.UnitRegistry.All
			.First(candidate => candidate.State.Phase == EPhase.InTransit
				|| candidate.State.IsReadyToDepart)
			.State;

		while (unit.Phase != EPhase.InTransit)
			orchestrator.AdvanceTick();

		var progressAfterDepart = unit.Journey.LongitudinalProgress;

		orchestrator.AdvanceTick();

		Assert.Equal(EPhase.InTransit, unit.Phase);
		Assert.True(unit.Journey.LongitudinalProgress > progressAfterDepart);
	}

	[Fact]
	public void AdvanceTicks_MinerVisitsExtractionAndRefinery()
	{
		var orchestrator = StarSystemOrchestrator.FromMap(StarMap.CreateDevDefault(42));
		var map = orchestrator.Map;
		var extractionDock = DockForRole(map, EPoiLogicalRole.Extraction).Id;
		var refineryDock = DockForRole(map, EPoiLogicalRole.Refinery).Id;
		var miner = FirstUnitOfType(map, EType.MiningBarge);
		var visited = new HashSet<string>(StringComparer.Ordinal);

		for (var tick = 0; tick < 1000; tick++)
		{
			orchestrator.AdvanceTick();
			if (miner.Phase == EPhase.Working)
				visited.Add(miner.DockedAtDockId);
		}

		Assert.Contains(extractionDock, visited);
		Assert.Contains(refineryDock, visited);
	}

	[Fact]
	public void AdvanceTicks_FreighterVisitsStorageAndExit()
	{
		var orchestrator = StarSystemOrchestrator.FromMap(StarMap.CreateDevDefault(42));
		var map = orchestrator.Map;
		var freighter = FirstUnitOfType(map, EType.ExportFreighter);
		var storageDock = DockForRole(map, EPoiLogicalRole.Storage).Id;
		var exitDock = DockForRole(map, EPoiLogicalRole.Exit).Id;
		var visited = new HashSet<string>(StringComparer.Ordinal);

		for (var tick = 0; tick < 1200; tick++)
		{
			orchestrator.AdvanceTick();
			if (freighter.Phase == EPhase.Working)
				visited.Add(freighter.DockedAtDockId);
		}

		Assert.Contains(storageDock, visited);
		Assert.Contains(exitDock, visited);
	}

	[Fact]
	public void AdvanceTicks_ComplianceVesselVisitsOperationalPoisAndReturnsHome()
	{
		var orchestrator = StarSystemOrchestrator.FromMap(StarMap.CreateDevDefault(42));
		var map = orchestrator.Map;
		var compliance = FirstUnitOfType(map, EType.ComplianceVessel);
		var adminDock = DockForRole(map, EPoiLogicalRole.Administrative).Id;
		var extractionDock = DockForRole(map, EPoiLogicalRole.Extraction).Id;
		var refineryDock = DockForRole(map, EPoiLogicalRole.Refinery).Id;
		var storageDock = DockForRole(map, EPoiLogicalRole.Storage).Id;
		var exitDock = DockForRole(map, EPoiLogicalRole.Exit).Id;
		var visited = new HashSet<string>(StringComparer.Ordinal);

		for (var tick = 0; tick < 3000; tick++)
		{
			orchestrator.AdvanceTick();
			if (compliance.Phase == EPhase.Working)
				visited.Add(compliance.DockedAtDockId);
		}

		Assert.Contains(extractionDock, visited);
		Assert.Contains(refineryDock, visited);
		Assert.Contains(storageDock, visited);
		Assert.Contains(exitDock, visited);
		Assert.Contains(adminDock, visited);
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

		var originalMiner = FirstUnitOfType(orchestrator.Map, EType.MiningBarge);
		var forkedMiner = FirstUnitOfType(forkedOrchestrator.Map, EType.MiningBarge);
		Assert.NotEqual(
			originalMiner.Journey.LongitudinalProgress,
			forkedMiner.Journey.LongitudinalProgress);
	}

	private static State FirstUnitOfType(StarMap map, EType type) =>
		map.UnitRegistry.All.First(unit => unit.State.Type == type).State;

	private static Dock DockForRole(StarMap map, EPoiLogicalRole role) =>
		map.DocksByPoiId[map.PointsOfInterest.Single(poi => poi.LogicalRole == role).Id];
}
