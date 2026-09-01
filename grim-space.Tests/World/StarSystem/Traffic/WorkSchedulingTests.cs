using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.Tests.World.StarSystem.Traffic;

public sealed class WorkSchedulingTests
{
	[Fact]
	public void ArrivalAtIdlePoi_StartsWorkImmediately()
	{
		var orchestrator = StarSystemTestHarness.CreateOrchestrator(42);
		var unit = orchestrator.Map.UnitRegistry.All.First(candidate => candidate.State.IsReadyToDepart);

		while (unit.State.Phase != EPhase.Working && orchestrator.Tick < 500)
			orchestrator.AdvanceTick();

		Assert.Equal(EPhase.Working, unit.State.Phase);
		Assert.True(unit.State.WorkStartTick > 0);
	}

	[Fact]
	public void MultipleArrivals_ReserveNonOverlappingFifoWindows()
	{
		var map = StarMap.CreateDevDefault(42);
		var poi = map.PointsOfInterest.Single(p => p.LogicalRole == EPoiLogicalRole.Extraction);
		var dockId = map.DocksByPoiId[poi.Id].Id;
		var units = map.UnitRegistry.All
			.Where(unit => unit.State.Type == EType.MiningBarge && unit.State.IsReadyToDepart)
			.Take(2)
			.ToArray();
		var runtime = new ActorRuntime();

		var first = ApplyReservation(map, runtime, units[0].State.Id, dockId);
		var second = ApplyReservation(map, runtime, units[1].State.Id, dockId);

		Assert.Equal(first.EndTick, second.StartTick);
		Assert.True(second.StartTick > first.StartTick);
	}

	[Fact]
	public void BeginAndCompleteWork_OccurAtScheduledTicks()
	{
		var map = StarMap.CreateDevDefault(42);
		if (map.Timeline.Clock.Current == 0)
			map.Timeline.Clock.Set(1);

		var poi = map.PointsOfInterest.Single(p => p.LogicalRole == EPoiLogicalRole.Extraction);
		var dockId = map.DocksByPoiId[poi.Id].Id;
		var unit = map.UnitRegistry.All.First(candidate =>
			candidate.State.Type == EType.MiningBarge
			&& candidate.State.DockedAtDockId == dockId
			&& candidate.State.Phase == EPhase.Docked);
		var duration = poi.DurationTicks(unit.State.Type);
		var currentTick = map.Timeline.Clock.Current;
		poi.NextAvailableTaskTick = currentTick + duration;

		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		actorRuntimes.Register(unit.State.Id, new ActorRuntime());
		var engine = new Engine<StarMap, ActorRuntime>(map, actorRuntimes);
		var reservation = ApplyReservation(map, actorRuntimes.For(unit.State.Id), unit.State.Id, dockId);

		Assert.Equal(EPhase.Docked, unit.State.Phase);
		Assert.Equal(currentTick + duration, reservation.StartTick);

		while (map.Timeline.Clock.Current < reservation.StartTick)
			engine.AdvanceTick();

		Assert.Equal(EPhase.Working, unit.State.Phase);

		while (map.Timeline.Clock.Current < reservation.EndTick)
			engine.AdvanceTick();

		Assert.Equal(EPhase.Docked, unit.State.Phase);
	}

	[Fact]
	public void SpawnedWorkingUnit_SchedulesCompletionOnOrchestratorInit()
	{
		var buildResult = StarSystemGenerator.Generate(42, EStarSystemClass.Supply);
		var workingUnit = buildResult.Map.UnitRegistry.All
			.First(unit => unit.State.Phase == EPhase.Working);
		var remaining = workingUnit.State.SpawnWorkRemainingTicks;

		var orchestrator = StarSystemTestHarness.CreateOrchestrator(buildResult.Map);

		Assert.Equal(EPhase.Working, workingUnit.State.Phase);
		orchestrator.AdvanceTicks(remaining);
		Assert.Equal(EPhase.Docked, workingUnit.State.Phase);
	}

	[Fact]
	public void Fork_PreservesReservationsAndPendingWorkActions()
	{
		var orchestrator = StarSystemTestHarness.CreateOrchestrator(42);
		orchestrator.AdvanceTick();

		var originalPoi = orchestrator.Map.PointsOfInterest
			.First(poi => poi.LogicalRole == EPoiLogicalRole.Extraction);
		var originalReservation = originalPoi.NextAvailableTaskTick;
		var originalMiner = orchestrator.Map.UnitRegistry.All
			.First(unit => unit.State.Type == EType.MiningBarge);

		var fork = orchestrator.Map.Fork();
		var forkedOrchestrator = StarSystemTestHarness.CreateOrchestrator(fork);
		var forkedPoi = fork.PointsOfInterest
			.First(poi => poi.LogicalRole == EPoiLogicalRole.Extraction);
		var forkedMiner = fork.UnitRegistry.All
			.First(unit => unit.State.Type == EType.MiningBarge);

		Assert.Equal(originalReservation, forkedPoi.NextAvailableTaskTick);
		Assert.Equal(originalMiner.State.WorkStartTick, forkedMiner.State.WorkStartTick);

		orchestrator.AdvanceTicks(10);
		forkedOrchestrator.AdvanceTicks(3);

		Assert.NotEqual(orchestrator.Tick, forkedOrchestrator.Tick);
	}

	private static WorkReservation ApplyReservation(
		StarMap map,
		ActorRuntime runtime,
		string unitId,
		string dockId)
	{
		var reservation = WorkScheduler.ReserveOnArrival(map, unitId, dockId);
		foreach (var effect in reservation.Effects)
			effect.Apply(map, runtime, unitId);

		return reservation;
	}
}
