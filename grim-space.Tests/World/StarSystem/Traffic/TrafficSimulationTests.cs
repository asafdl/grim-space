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
		var miner = map.UnitRegistry.UnitOf(SupplySystemGenerator.MinerOneId).State;

		orchestrator.AdvanceTick();

		Assert.Equal(EPhase.InTransit, miner.Phase);
		Assert.NotNull(miner.Journey.RouteId);
		Assert.Contains(
			SupplySystemGenerator.MinerOneId,
			map.TrafficController.OccupantsByRouteId[miner.Journey.RouteId!]);

		orchestrator.AdvanceTick();

		Assert.Equal(EPhase.InTransit, miner.Phase);
		Assert.True(miner.Journey.LongitudinalProgress > 0);
	}

	[Fact]
	public void AdvanceTick_AdvancesJourneyProgressWhileInTransit()
	{
		var orchestrator = StarSystemOrchestrator.FromMap(StarMap.CreateDevDefault(42));
		var miner = orchestrator.Map.UnitRegistry.UnitOf(SupplySystemGenerator.MinerOneId).State;

		orchestrator.AdvanceTicks(2);
		var progressAfterDepart = miner.Journey.LongitudinalProgress;

		orchestrator.AdvanceTick();

		Assert.Equal(EPhase.InTransit, miner.Phase);
		Assert.True(miner.Journey.LongitudinalProgress > progressAfterDepart);
	}

	[Fact]
	public void AdvanceTicks_MinerVisitsExtractionAndRefinery()
	{
		var orchestrator = StarSystemOrchestrator.FromMap(StarMap.CreateDevDefault(42));
		var map = orchestrator.Map;
		var extractionDock = DockForRole(map, EPoiLogicalRole.Extraction).Id;
		var refineryDock = DockForRole(map, EPoiLogicalRole.Refinery).Id;
		var miner = map.UnitRegistry.UnitOf(SupplySystemGenerator.MinerOneId).State;
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
		var freighter = map.UnitRegistry.UnitOf(SupplySystemGenerator.FreighterId).State;
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

		var originalMiner = orchestrator.Map.UnitRegistry.UnitOf(SupplySystemGenerator.MinerOneId).State;
		var forkedMiner = forkedOrchestrator.Map.UnitRegistry.UnitOf(SupplySystemGenerator.MinerOneId).State;
		Assert.NotEqual(
			originalMiner.Journey.LongitudinalProgress,
			forkedMiner.Journey.LongitudinalProgress);
	}

	private static Dock DockForRole(StarMap map, EPoiLogicalRole role) =>
		map.DocksByPoiId[map.PointsOfInterest.Single(poi => poi.LogicalRole == role).Id];
}
