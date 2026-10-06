using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.World.StarSystem.Vision;

namespace GrimSpace.World.StarSystem.Contact;

public sealed class PursuitPlanner
{
	private readonly Func<StarMap> _world;
	private readonly Func<string, ActorRuntime> _runtimeFor;
	private readonly IPathfinder _pathfinder;

	public PursuitPlanner(
		Func<StarMap> world,
		Func<string, ActorRuntime> runtimeFor,
		IPathfinder pathfinder)
	{
		_world = world;
		_runtimeFor = runtimeFor;
		_pathfinder = pathfinder;
	}

	public PursueContactAction? Plan(string actorId)
	{
		var world = _world();
		var runtime = _runtimeFor(actorId);
		if (!world.FleetRegistry.TryGet(actorId, out var actor))
			return null;

		var directive = actor.State.PursuitDirective;
		if (directive is null
			|| !world.FleetRegistry.TryGet(directive.TargetFleetId, out var target))
			return null;

		var origin = MoveDef.ResolveOrigin(world, actor, runtime);
		var destination = PursuitDestination(world, target, _runtimeFor);
		if (EngagementQueries.IsHunterInEngageRange(
				origin,
				destination,
				actor.State.EngageRadius))
			return null;

		if (IsActivelyPursuing(
				world,
				actor.State,
				target.State,
				runtime,
				directive.TargetFleetId,
				destination))
			return null;

		if (_pathfinder.FindPath(origin, destination) is not PathfindingResult.Found found)
			return null;

		var pursue = new PursueContactAction(
			actorId,
			new FleetContactTarget(directive.TargetFleetId),
			destination,
			found.Path,
			EContactIntent.Engagement);
		return PursueContactDef.Instance.IsLegal(pursue, world, runtime) ? pursue : null;
	}

	private static Coord PursuitDestination(
		StarMap world,
		Fleet target,
		Func<string, ActorRuntime> runtimeFor)
	{
		var sample = FleetPositionSampler.Sample(
			world,
			target.State,
			runtimeFor(target.State.Id),
			0f);
		return new Coord(
			(int)System.Math.Round(sample.X),
			0,
			(int)System.Math.Round(sample.Z));
	}

	private static bool IsActivelyPursuing(
		StarMap world,
		State state,
		State targetState,
		ActorRuntime runtime,
		string targetFleetId,
		Coord targetDestination)
	{
		if (!state.TravelTarget.MatchesFleet(targetFleetId, EContactIntent.Engagement)
			|| state.Phase != EPhase.InTransit
			|| runtime.CachedPath is null)
			return false;

		if (state.Journey.Destination == targetDestination)
			return true;

		var maxClosingSpeed = EngagementQueries.MaximumTravelSpeed(state)
			+ EngagementQueries.MaximumTravelSpeed(targetState);
		var delay = EngagementQueries.ContactCheckDelay(
			state.Journey.Origin,
			state.Journey.Destination,
			state.EngageRadius,
			maxClosingSpeed);
		return world.Timeline.Clock.Current - state.Journey.StartTick < delay;
	}
}
