using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Traffic;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.Tests.World.StarSystem.Traffic;

public sealed class TrafficSimulationTests
{
	[Fact]
	public void AdvanceTick_DepartsDockedUnitWithResolvedPath()
	{
		var orchestrator = StarSystemTestHarness.CreateOrchestrator(42);
		var map = orchestrator.Map;
		var readyUnit = map.UnitRegistry.All.First(unit => unit.State.IsReadyToDepart);
		var runtime = orchestrator.RuntimeFor(readyUnit.State.Id);

		orchestrator.AdvanceTick();

		Assert.Equal(EPhase.InTransit, readyUnit.State.Phase);
		Assert.NotNull(runtime.CachedPath);
		Assert.NotEqual(0, readyUnit.State.Journey.JourneyId);

		orchestrator.AdvanceTick();

		Assert.Equal(EPhase.InTransit, readyUnit.State.Phase);
		var (position, _) = readyUnit.State.CommittedPosition(
			map,
			runtime.CachedPath,
			0f);
		Assert.NotEqual(readyUnit.State.Journey.Origin, position);
	}

	[Fact]
	public void AdvanceTick_AdvancesJourneyProgressWhileInTransit()
	{
		var orchestrator = StarSystemTestHarness.CreateOrchestrator(42);
		var unit = orchestrator.Map.UnitRegistry.All
			.First(candidate => candidate.State.Phase == EPhase.InTransit
				|| candidate.State.IsReadyToDepart);

		while (unit.State.Phase != EPhase.InTransit)
			orchestrator.AdvanceTick();

		var map = orchestrator.Map;
		var path = orchestrator.RuntimeFor(unit.State.Id).CachedPath!;
		var positionAfterDepart = unit.State.CommittedPosition(map, path, 0f).Position;

		orchestrator.AdvanceTick();

		Assert.Equal(EPhase.InTransit, unit.State.Phase);
		var positionAfterTick = unit.State.CommittedPosition(map, path, 0f).Position;
		Assert.NotEqual(positionAfterDepart, positionAfterTick);
	}

	[Fact]
	public void AdvanceTicks_MinerVisitsExtractionAndRefinery()
	{
		var orchestrator = StarSystemTestHarness.CreateOrchestrator(42);
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
		var orchestrator = StarSystemTestHarness.CreateOrchestrator(42);
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
		var orchestrator = StarSystemTestHarness.CreateOrchestrator(42);
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
	public void AdvanceTicks_200TickLoop_DoesNotThrow()
	{
		var orchestrator = StarSystemTestHarness.CreateOrchestrator(7);

		orchestrator.AdvanceTicks(200);

		Assert.Equal(201, orchestrator.Tick);
	}

	[Fact]
	public void Fork_KeepsTrafficStateIndependent()
	{
		var orchestrator = StarSystemTestHarness.CreateOrchestrator(11);
		orchestrator.AdvanceTicks(25);

		var forkedMap = orchestrator.Map.Fork();
		var forkedOrchestrator = StarSystemTestHarness.CreateOrchestrator(forkedMap);
		forkedOrchestrator.AdvanceTicks(10);

		Assert.NotSame(orchestrator.Map.Timeline, forkedOrchestrator.Map.Timeline);
		Assert.NotSame(orchestrator.Map.UnitRegistry, forkedOrchestrator.Map.UnitRegistry);
		Assert.Same(orchestrator.Map.RoutesById, forkedOrchestrator.Map.RoutesById);

		var originalMiner = orchestrator.Map.UnitRegistry.UnitOf(
			FirstUnitOfType(orchestrator.Map, EType.MiningBarge).Id);
		var forkedMiner = forkedOrchestrator.Map.UnitRegistry.UnitOf(
			FirstUnitOfType(forkedOrchestrator.Map, EType.MiningBarge).Id);

		if (originalMiner.State.Phase == EPhase.InTransit
			&& forkedMiner.State.Phase == EPhase.InTransit)
		{
			var originalPosition = originalMiner.State.CommittedPosition(
				orchestrator.Map,
				orchestrator.RuntimeFor(originalMiner.State.Id).CachedPath,
				0f).Position;
			var forkedPosition = forkedMiner.State.CommittedPosition(
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
		map.UnitRegistry.All.First(unit => unit.State.Type == type).State;

	private static Dock DockForRole(StarMap map, EPoiLogicalRole role) =>
		map.DocksByPoiId[map.PointsOfInterest.Single(poi => poi.LogicalRole == role).Id];
}
