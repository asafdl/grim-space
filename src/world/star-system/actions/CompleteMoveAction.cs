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
		&& world.FleetRegistry.TryGet(complete.UnitId, out var unit)
		&& unit.State.Travel is FleetTravel.Journey journey
		&& journey.Id == complete.JourneyId;

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var complete = (CompleteMoveAction)action;
		if (!world.FleetRegistry.TryGet(complete.UnitId, out var unit)
			|| unit.State.Travel is not FleetTravel.Journey journey
			|| journey.Id != complete.JourneyId)
		{
			return [];
		}

		var destination = journey.Destination;
		var effects = new List<IEffect<StarMap, ActorRuntime>>
		{
			ClearJourneyRuntimeEffect.Instance,
			UpdateLocationEffect.StopAt(complete.UnitId, destination),
		};

		if (world.DocksByPosition.TryGetValue(destination, out var dock))
		{
			if (unit.State.ChoreDockIds.Count > 0)
				effects.Add(new ReserveWorkOnArrivalEffect(complete.UnitId, dock.Id));

			return effects;
		}

		return effects;
	}
}
