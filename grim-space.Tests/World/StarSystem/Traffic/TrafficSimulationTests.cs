using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Traffic;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Traffic;

[StarSystemTestSuite]
public sealed class TrafficSimulationTests(StarMapFixture maps)
{
	[Fact]
	public void AdvanceTick_DepartsDockedUnitWithResolvedPath()
	{
		var orchestrator = StarSystemTestHarness.CreateOrchestrator(maps, 42);
		var map = orchestrator.Map;
		var readyUnit = map.FleetRegistry.All.First(unit => unit.State.HasChoreAtDock(map));
		var runtime = orchestrator.RuntimeFor(readyUnit.State.Id);

		orchestrator.AdvanceTick();

		var journey = readyUnit.State.Journey();
		Assert.NotNull(runtime.CachedPath);
		Assert.NotEqual(0, journey.Id);

		orchestrator.AdvanceTick();

		journey = readyUnit.State.Journey();
		var (position, _) = readyUnit.State.PositionAt(
			map,
			runtime.CachedPath,
			0f);
		Assert.NotEqual(journey.Origin, position);
	}

	[Fact]
	public void AdvanceTick_AdvancesJourneyProgressWhileInTransit()
	{
		var orchestrator = StarSystemTestHarness.CreateOrchestrator(maps, 42);
		var unit = orchestrator.Map.FleetRegistry.All
			.First(candidate => candidate.State.Travel is FleetTravel.Journey
				|| candidate.State.HasChoreAtDock(orchestrator.Map));

		while (unit.State.Travel is not FleetTravel.Journey)
			orchestrator.AdvanceTick();

		var map = orchestrator.Map;
		var path = orchestrator.RuntimeFor(unit.State.Id).CachedPath!;
		var positionAfterDepart = unit.State.PositionAt(map, path, 0f).Position;

		orchestrator.AdvanceTick();

		Assert.IsType<FleetTravel.Journey>(unit.State.Travel);
		var positionAfterTick = unit.State.PositionAt(map, path, 0f).Position;
		Assert.NotEqual(positionAfterDepart, positionAfterTick);
	}

	[Fact]
	public void AdvanceTicks_MinerVisitsExtractionAndRefinery()
	{
		var orchestrator = StarSystemTestHarness.CreateOrchestrator(maps, 42);
		var map = orchestrator.Map;
		var extractionDock = DockForRole(map, EPoiLogicalRole.Extraction).Id;
		var refineryDock = DockForRole(map, EPoiLogicalRole.Refinery).Id;
		var miner = FirstUnitOfType(map, EType.MiningBarge);
		var visited = new HashSet<string>(StringComparer.Ordinal);

		for (var tick = 0; tick < 1000; tick++)
		{
			orchestrator.AdvanceTick();
			if (WorkScheduler.IsWorking(map, miner.Id))
				visited.Add(map.DockAt(miner)!.Id);
		}

		Assert.Contains(extractionDock, visited);
		Assert.Contains(refineryDock, visited);
	}

	[Fact]
	public void AdvanceTicks_FreighterVisitsStorageAndExit()
	{
		var orchestrator = StarSystemTestHarness.CreateOrchestrator(maps, 42);
		var map = orchestrator.Map;
		var freighter = FirstUnitOfType(map, EType.ExportFreighter);
		var storageDock = DockForRole(map, EPoiLogicalRole.Storage).Id;
		var exitDock = DockForRole(map, EPoiLogicalRole.Exit).Id;
		var visited = new HashSet<string>(StringComparer.Ordinal);

		for (var tick = 0; tick < 1200; tick++)
		{
			orchestrator.AdvanceTick();
			if (WorkScheduler.IsWorking(map, freighter.Id))
				visited.Add(map.DockAt(freighter)!.Id);
		}

		Assert.Contains(storageDock, visited);
		Assert.Contains(exitDock, visited);
	}

	[Fact]
	public void AdvanceTicks_ComplianceVesselVisitsOperationalPoisAndReturnsHome()
	{
		var orchestrator = StarSystemTestHarness.CreateOrchestrator(maps, 42);
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
			if (WorkScheduler.IsWorking(map, compliance.Id))
				visited.Add(map.DockAt(compliance)!.Id);
		}

		Assert.Contains(extractionDock, visited);
		Assert.Contains(refineryDock, visited);
		Assert.Contains(storageDock, visited);
		Assert.Contains(exitDock, visited);
		Assert.Contains(adminDock, visited);
	}

	[Fact]
	public void AdvanceTicks_200TickLoop_DoesNotThrow()
	{
		var orchestrator = StarSystemTestHarness.CreateOrchestrator(maps, 7);

		orchestrator.AdvanceTicks(200);

		Assert.Equal(201, orchestrator.Tick);
	}

	[Fact]
	public void Fork_KeepsTrafficStateIndependent()
	{
		var orchestrator = StarSystemTestHarness.CreateOrchestrator(maps, 11);
		orchestrator.AdvanceTicks(25);

		var forkedMap = orchestrator.Map.Fork();
		var forkedOrchestrator = StarSystemTestHarness.CreateOrchestrator(forkedMap);
		forkedOrchestrator.AdvanceTicks(10);

		Assert.NotSame(orchestrator.Map.Timeline, forkedOrchestrator.Map.Timeline);
		Assert.NotSame(orchestrator.Map.FleetRegistry, forkedOrchestrator.Map.FleetRegistry);
		Assert.Same(orchestrator.Map.RoutesById, forkedOrchestrator.Map.RoutesById);

		var originalMiner = orchestrator.Map.FleetRegistry.FleetOf(
			FirstUnitOfType(orchestrator.Map, EType.MiningBarge).Id);
		var forkedMiner = forkedOrchestrator.Map.FleetRegistry.FleetOf(
			FirstUnitOfType(forkedOrchestrator.Map, EType.MiningBarge).Id);

		if (originalMiner.State.Travel is FleetTravel.Journey
			&& forkedMiner.State.Travel is FleetTravel.Journey)
		{
			var originalPosition = originalMiner.State.PositionAt(
				orchestrator.Map,
				orchestrator.RuntimeFor(originalMiner.State.Id).CachedPath,
				0f).Position;
			var forkedPosition = forkedMiner.State.PositionAt(
				forkedOrchestrator.Map,
				forkedOrchestrator.RuntimeFor(forkedMiner.State.Id).CachedPath,
				0f).Position;
			Assert.NotEqual(originalPosition, forkedPosition);
		}
		else
		{
			Assert.NotEqual(orchestrator.Tick, forkedOrchestrator.Tick);
		}
	}

	private static State FirstUnitOfType(StarMap map, EType type) =>
		map.FleetRegistry.All.First(unit => unit.State.Type == type).State;

	private static Dock DockForRole(StarMap map, EPoiLogicalRole role) =>
		map.DocksByPoiId[map.PointsOfInterest.Single(poi => poi.LogicalRole == role).Id];
}
