using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record CompleteMoveAction(
	string ActorId,
	string UnitId,
	long JourneyId) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		CompleteMoveDef.Instance;
}

public sealed class CompleteMoveDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static CompleteMoveDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is CompleteMoveAction complete
		&& world.UnitRegistry.TryGet(complete.UnitId, out var unit)
		&& unit.State.Phase == EPhase.InTransit
		&& unit.State.Journey.JourneyId == complete.JourneyId;

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var complete = (CompleteMoveAction)action;
		if (!world.UnitRegistry.TryGet(complete.UnitId, out var unit)
			|| unit.State.Phase != EPhase.InTransit
			|| unit.State.Journey.JourneyId != complete.JourneyId)
		{
			return [];
		}

		var destination = unit.State.Journey.Destination;
		var effects = new List<IEffect<StarMap, ActorRuntime>>
		{
			ClearJourneyRuntimeEffect.Instance,
		};

		if (world.DocksByPosition.TryGetValue(destination, out var dock))
		{
			effects.Add(UpdateLocationEffect.ArriveAtDock(complete.UnitId, dock.Id));
			var reservation = WorkScheduler.ReserveOnArrival(world, complete.UnitId, dock.Id);
			effects.AddRange(reservation.Effects);
			return effects;
		}

		effects.Add(UpdateLocationEffect.ArriveAtCoord(complete.UnitId, destination));
		return effects;
	}
}
