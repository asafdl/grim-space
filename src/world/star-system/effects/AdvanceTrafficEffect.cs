using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class AdvanceTrafficEffect : IEffect<StarMap, EmptyRuntime>
{
	public IReadOnlyList<IRecord> Apply(StarMap world, EmptyRuntime runtime, string actorId)
	{
		foreach (var unit in world.UnitRegistry.All)
		{
			if (unit.State.Phase == EPhase.InTransit)
				AdvanceTransit(world, unit.State);
		}

		foreach (var poi in world.PointsOfInterest)
		{
			if (poi.HasTasks)
				poi.AdvanceTick(world.UnitRegistry);
		}

		return [];
	}

	public void Undo(StarMap world, EmptyRuntime runtime, string actorId) { }

	private static void AdvanceTransit(StarMap world, State state)
	{
		if (!state.AdvanceTransit(state.SpeedPerTick))
			return;

		var destinationDockId = state.Journey.DestinationDockId
			?? throw new InvalidOperationException($"Unit '{state.Id}' completed transit without a destination.");

		state.ArriveAt(destinationDockId);
		state.EnterWaiting();
		var poiId = world.DocksById[destinationDockId].PoiId;
		var poi = world.PointsOfInterest.First(p => p.Id == poiId);
		if (!poi.HasTasks)
		{
			throw new InvalidOperationException($"POI '{poiId}' has no task supplier.");
		}

		poi.Enqueue(state.Id);
		state.AdvanceChoreIndex();
	}
}
