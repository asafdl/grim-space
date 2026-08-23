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
			var state = unit.State;
			switch (state.Phase)
			{
				case EPhase.InTransit:
					AdvanceTransit(world, state);
					break;
				case EPhase.Working:
					state.TickWork();
					break;
			}

			if (state.IsReadyToDepart)
				TryDepart(world, state);
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
