using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record ReturnToPatrolAction(string ActorId) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		ReturnToPatrolDef.Instance;
}

public sealed class ReturnToPatrolDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static ReturnToPatrolDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is ReturnToPatrolAction patrol
		&& world.FleetRegistry.TryGet(patrol.ActorId, out var fleet)
		&& fleet.State.PatrolRadius > 0
		&& fleet.State.PursuitDirective is null
		&& fleet.State.CurrentEngagement is null
		&& TryPlanReturn(world, fleet, runtime, out _, out _);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var patrol = (ReturnToPatrolAction)action;
		var fleet = world.FleetRegistry.FleetOf(patrol.ActorId);
		if (!TryPlanReturn(world, fleet, runtime, out var origin, out var path))
			throw new InvalidOperationException(
				$"Fleet '{patrol.ActorId}' cannot return to patrol origin '{fleet.State.PatrolOrigin}'.");

		if (origin == fleet.State.PatrolOrigin)
			return [new StopAtCurrentLocationEffect(patrol.ActorId)];

		return
		[
			CancelPendingMoveEffect.Instance,
			.. MovementEffects.BeginJourney(
				patrol.ActorId,
				runtime,
				world,
				origin,
				fleet.State.PatrolOrigin,
				path),
		];
	}

	private static bool TryPlanReturn(
		StarMap world,
		Fleet fleet,
		ActorRuntime runtime,
		out GrimSpace.Math.Grid.Coord origin,
		out TransitPath path)
	{
		origin = MoveDef.ResolveOrigin(world, fleet, runtime);
		if (origin == fleet.State.PatrolOrigin)
		{
			path = null!;
			return true;
		}

		var result = new GridPathfinder(world.PathfindingTerrain)
			.FindPath(origin, fleet.State.PatrolOrigin);
		if (result is PathfindingResult.Found found)
		{
			path = found.Path;
			return true;
		}

		path = null!;
		return false;
	}
}
