using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.World.StarSystem.Vision;

namespace GrimSpace.World.StarSystem.Contact;

public sealed record PursuitCourse(Coord Destination, TransitPath Path);

public sealed class PursuitPlanner
{
	private const int MaxInterceptSamples = 32;

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
			|| !world.FleetRegistry.TryGet(directive.TargetFleetId, out var target)
			|| world.DockAt(target.State) is not null)
			return null;

		var origin = actor.State.PositionAt(world, runtime.CachedPath, 0).Position;
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

	public PursuitCourse? PlanInterceptCourse(
		string actorId,
		string targetFleetId,
		EContactIntent intent)
	{
		var world = _world();
		var runtime = _runtimeFor(actorId);
		if (!world.FleetRegistry.TryGet(actorId, out var actor)
			|| !world.FleetRegistry.TryGet(targetFleetId, out var target))
			return null;

		var origin = actor.State.PositionAt(world, runtime.CachedPath, 0).Position;
		var targetRuntime = _runtimeFor(targetFleetId);
		TransitCache.RebuildIfMissing(target, targetRuntime, _pathfinder);
		if (target.State.Travel is not FleetTravel.Journey targetJourney
			|| targetRuntime.CachedPath is not { } targetPath)
			return FindCourse(origin, PursuitDestination(world, target, _runtimeFor));

		var elapsed = world.Timeline.Clock.Current - targetJourney.StartTick;
		var remaining = System.Math.Max(
			0.0,
			targetPath.TicksRequired(target.State.SpeedPerTick) - elapsed);
		if (remaining <= 0.0)
			return FindCourse(origin, targetJourney.Destination);

		PursuitCourse? finalCourse = null;
		var sampleCount = System.Math.Min(
			MaxInterceptSamples,
			System.Math.Max(1, (int)System.Math.Ceiling(remaining)));
		var pursuitSpeedMultiplier = EngagementQueries.PursuitSpeedMultiplier(actor.State, intent);

		for (var sampleIndex = 1; sampleIndex <= sampleCount; sampleIndex++)
		{
			var futureTicks = remaining * sampleIndex / sampleCount;
			var destination = targetPath
				.SampleAtElapsed(elapsed + futureTicks, target.State.SpeedPerTick)
				.Position;
			var course = FindCourse(origin, destination);
			if (course is null)
				continue;

			finalCourse = course;
			var travelTicks = course.Path
				.WithSpeedMultiplier(pursuitSpeedMultiplier)
				.TicksRequired(actor.State.SpeedPerTick);
			if (travelTicks <= futureTicks)
				return course;
		}

		return finalCourse;
	}

	private PursuitCourse? FindCourse(Coord origin, Coord destination) =>
		_pathfinder.FindPath(origin, destination) is PathfindingResult.Found found
			? new PursuitCourse(destination, found.Path)
			: null;

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
			|| state.Travel is not FleetTravel.Journey journey
			|| runtime.CachedPath is null)
			return false;

		if (journey.Destination == targetDestination)
			return true;

		var maxClosingSpeed = EngagementQueries.MaximumTravelSpeed(state)
			+ EngagementQueries.MaximumTravelSpeed(targetState);
		var delay = EngagementQueries.ContactCheckDelay(
			journey.Origin,
			journey.Destination,
			state.EngageRadius,
			maxClosingSpeed);
		return world.Timeline.Clock.Current - journey.StartTick < delay;
	}
}
