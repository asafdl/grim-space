using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Traffic;

[StarSystemTestSuite]
public sealed class WorkSchedulingTests(StarMapFixture maps)
{
	[Fact]
	public void ArrivalAtIdlePoi_StartsWorkImmediately()
	{
		var orchestrator = StarSystemTestHarness.CreateOrchestrator(maps, 42);
		var map = orchestrator.Map;
		var unit = map.FleetRegistry.All.First(candidate => candidate.State.HasChoreAtDock(map));

		while (!WorkScheduler.IsWorking(map, unit.State.Id) && orchestrator.Tick < 500)
			orchestrator.AdvanceTick();

		Assert.True(WorkScheduler.IsWorking(map, unit.State.Id));
	}

	[Fact]
	public void MultipleArrivals_ReserveNonOverlappingFifoWindows()
	{
		var map = maps.Fresh(42);
		var poi = map.PointsOfInterest.Single(p => p.LogicalRole == EPoiLogicalRole.Extraction);
		var dockId = map.DocksByPoiId[poi.Id].Id;
		var units = map.FleetRegistry.All
			.Where(unit => unit.State.Type == EType.MiningBarge && unit.State.HasChoreAtDock(map))
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
		var map = maps.Fresh(42);
		if (map.Timeline.Clock.Current == 0)
			map.Timeline.Clock.Set(1);

		var poi = map.PointsOfInterest.Single(p => p.LogicalRole == EPoiLogicalRole.Extraction);
		var dockId = map.DocksByPoiId[poi.Id].Id;
		var unit = map.FleetRegistry.All.First(candidate => candidate.State.Type == EType.MiningBarge);
		map.Timeline.CancelPendingForActor(unit.State.Id);
		unit.State.Travel = new FleetTravel.AtRest(map.DocksById[dockId].Position);
		var duration = poi.DurationTicks(unit.State.Type);
		var currentTick = map.Timeline.Clock.Current;
		poi.NextAvailableTaskTick = currentTick + duration;

		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		actorRuntimes.Register(unit.State.Id, new ActorRuntime());
		var engine = new Engine<StarMap, ActorRuntime>(map, actorRuntimes);
		var reservation = ApplyReservation(map, actorRuntimes.For(unit.State.Id), unit.State.Id, dockId);

		Assert.False(WorkScheduler.IsWorking(map, unit.State.Id));
		Assert.Equal(currentTick + duration, reservation.StartTick);

		while (map.Timeline.Clock.Current < reservation.StartTick)
			engine.AdvanceTick();

		Assert.True(WorkScheduler.IsWorking(map, unit.State.Id));

		while (map.Timeline.Clock.Current < reservation.EndTick)
			engine.AdvanceTick();

		Assert.False(WorkScheduler.IsWorking(map, unit.State.Id));
	}

	[Fact]
	public void SpawnedWorkingUnit_HasScheduledCompletion()
	{
		var map = StarSystemGenerator.Generate(42, EStarSystemClass.Supply);
		var workingUnit = map.FleetRegistry.All
			.First(unit => WorkScheduler.IsWorking(map, unit.State.Id));
		var completionTick = map.Timeline.ToSnapshot().Pending
			.Single(pair => pair.Value.Any(action =>
				action is CompleteWorkAction complete
				&& complete.UnitId == workingUnit.State.Id))
			.Key;
		var remaining = completionTick - map.Timeline.Clock.Current;

		var orchestrator = StarSystemTestHarness.CreateOrchestrator(map);

		Assert.True(WorkScheduler.IsWorking(map, workingUnit.State.Id));
		orchestrator.AdvanceTicks(remaining);
		Assert.False(WorkScheduler.IsWorking(map, workingUnit.State.Id));
	}

	[Fact]
	public void Fork_PreservesReservationsAndPendingWorkActions()
	{
		var orchestrator = StarSystemTestHarness.CreateOrchestrator(maps, 42);
		orchestrator.AdvanceTick();

		var originalPoi = orchestrator.Map.PointsOfInterest
			.First(poi => poi.LogicalRole == EPoiLogicalRole.Extraction);
		var originalReservation = originalPoi.NextAvailableTaskTick;
		var fork = orchestrator.Map.Fork();
		var forkedOrchestrator = StarSystemTestHarness.CreateOrchestrator(fork);
		var forkedPoi = fork.PointsOfInterest
			.First(poi => poi.LogicalRole == EPoiLogicalRole.Extraction);

		Assert.Equal(originalReservation, forkedPoi.NextAvailableTaskTick);
		Assert.Equal(
			orchestrator.Map.Timeline.ToSnapshot().Pending,
			fork.Timeline.ToSnapshot().Pending);

		orchestrator.AdvanceTicks(10);
		forkedOrchestrator.AdvanceTicks(3);

		Assert.NotEqual(orchestrator.Tick, forkedOrchestrator.Tick);
	}

	private static WorkReservation ApplyReservation(
		StarMap map,
		ActorRuntime runtime,
		string unitId,
		string dockId)
		=> WorkScheduler.ReserveOnArrival(map, unitId, dockId);
}
