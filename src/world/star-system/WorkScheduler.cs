using GrimSpace.Core.Actions;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem;

public sealed record WorkReservation(
	int StartTick,
	int EndTick);

public static class WorkScheduler
{
	public static WorkReservation ReserveOnArrival(
		StarMap world,
		string unitId,
		string dockId)
	{
		var state = world.StateOf(unitId);
		state.AdvanceChoreIndex();

		var poiId = world.DocksById[dockId].PoiId;
		var poi = PoiOf(world, poiId);
		if (!poi.HasTasks)
		{
			throw new InvalidOperationException($"POI '{poiId}' has no task supplier.");
		}

		var duration = poi.DurationTicks(state.Type);
		var currentTick = world.Timeline.Clock.Current;
		var (startTick, endTick) = poi.ReserveTaskWindow(currentTick, duration);

		if (startTick > currentTick)
			Schedule(world, startTick - currentTick, new BeginWorkAction(unitId, unitId, poiId, startTick));

		Schedule(
			world,
			endTick - currentTick,
			new CompleteWorkAction(unitId, unitId, poiId, startTick));

		return new WorkReservation(startTick, endTick);
	}

	public static void ScheduleSpawnedWorker(
		StarMap world,
		Fleet unit,
		string poiId,
		int remainingTicks)
	{
		var poi = PoiOf(world, poiId);
		var duration = poi.DurationTicks(unit.State.Type);
		var currentTick = world.Timeline.Clock.Current;
		var endTick = currentTick + remainingTicks;
		var startTick = endTick - duration;

		poi.ExtendReservation(endTick);
		Schedule(
			world,
			remainingTicks,
			new CompleteWorkAction(unit.State.Id, unit.State.Id, poiId, startTick));
	}

	public static bool HasAssignment(StarMap world, string unitId) =>
		world.Timeline.ContainsPending(action =>
			action is CompleteWorkAction complete && complete.UnitId == unitId);

	public static bool IsWorking(StarMap world, string unitId) =>
		world.Timeline.ContainsPending(action =>
			action is CompleteWorkAction complete
			&& complete.UnitId == unitId
			&& complete.StartTick <= world.Timeline.Clock.Current);

	public static bool IsAssigned(
		StarMap world,
		string unitId,
		string poiId,
		int startTick) =>
		world.Timeline.ContainsPending(action =>
			action is CompleteWorkAction complete
			&& complete.UnitId == unitId
			&& complete.PoiId == poiId
			&& complete.StartTick == startTick);

	private static void Schedule(StarMap world, int delayTicks, IAction action) =>
		world.Timeline.Schedule(delayTicks, action);

	private static PointOfInterest PoiOf(StarMap world, string poiId) =>
		world.PointsOfInterest.First(poi => poi.Id == poiId);
}
