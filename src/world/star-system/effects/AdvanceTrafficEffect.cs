using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Traffic;
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

		foreach (var unit in world.UnitRegistry.All)
		{
			if (unit.State.IsReadyToDepart)
				TryDepart(world, unit.State);
		}

		world.TrafficController.Validate();
		return [];
	}

	public void Undo(StarMap world, EmptyRuntime runtime, string actorId) { }

	private static void AdvanceTransit(StarMap world, State state)
	{
		var routeId = state.Journey.RouteId
			?? throw new InvalidOperationException($"Unit '{state.Id}' is in transit without a route.");

		var route = world.RoutesById[routeId];
		state.AdvanceTransit(state.SpeedPerTick);

		if (state.Journey.LongitudinalProgress < route.Length)
			return;

		world.TrafficController.UnregisterLane(state.Id);
		var destinationDockId = SystemTrafficController.DestinationDock(route, state.Journey.TowardDockB);
		state.ArriveAt(destinationDockId);
		state.EnterWaiting();
		var poiId = world.DocksById[destinationDockId].PoiId;
		var poi = world.PointsOfInterest.First(p => p.Id == poiId);
		if (!poi.HasTasks)
			throw new InvalidOperationException($"POI '{poiId}' has no task supplier.");
		poi.Enqueue(state.Id);
		state.AdvanceChoreIndex();
	}

	private static void TryDepart(StarMap world, State state)
	{
		var destinationDockId = state.NextChoreDockId();
		if (!world.TryResolveRoute(state.DockedAtDockId, destinationDockId, out var route, out var towardDockB))
			return;

		if (!world.TrafficController.VerifyLane(state.Id, route.Id, towardDockB))
			return;

		if (!world.TrafficController.TryRegisterLane(state.Id, route.Id, towardDockB))
			return;

		state.BeginTransit(route.Id, towardDockB);
	}
}
