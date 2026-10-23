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
	private const int MaxInterceptSamples = 8;

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

		var targetFleetId = ResolvePursuitTargetFleetId(actor.State);
		if (targetFleetId is null
			|| !world.FleetRegistry.TryGet(targetFleetId, out var target)
			|| world.DockAt(target.State) is not null)
			return null;

		const EContactIntent intent = EContactIntent.Engagement;
		var origin = actor.State.PositionAt(world, runtime.CachedPath, 0).Position;
		var targetPosition = PursuitDestination(world, target, _runtimeFor);
		if (EngagementQueries.IsHunterInEngageRange(
				origin,
				targetPosition,
				actor.State.EngageRadius))
			return null;

		if (ShouldDeferReplan(world, actor.State, target.State, runtime, targetFleetId, targetPosition))
			return null;

		var course = PlanInterceptCourse(actorId, targetFleetId, intent);
		if (course is null)
			return null;

		if (IsActivelyPursuing(
				world,
				actor.State,
				target.State,
				runtime,
				targetFleetId,
				course.Destination))
			return null;

		var pursue = new PursueContactAction(
			actorId,
			new FleetContactTarget(targetFleetId),
			course.Destination,
			course.Path,
			intent);
		return PursueContactDef.Instance.IsLegal(pursue, world, runtime) ? pursue : null;
	}

	private static string? ResolvePursuitTargetFleetId(State state)
	{
		if (state.PursuitDirective is { } directive)
			return directive.TargetFleetId;

		if (state.CurrentEngagement is { Phase: EEngagementPhase.Pursuing, Hunting: var hunting })
			return hunting;

		return null;
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
		{
			return FindCourse(origin, PursuitDestination(world, target, _runtimeFor));
		}

		var elapsed = world.Timeline.Clock.Current - targetJourney.StartTick;
		var remaining = System.Math.Max(
			0.0,
			targetPath.TicksRequired(target.State.SpeedPerTick) - elapsed);
		if (remaining <= 0.0)
		{
			return FindCourse(origin, targetJourney.Destination);
		}

		var sampleCount = System.Math.Min(
			MaxInterceptSamples,
			System.Math.Max(1, (int)System.Math.Ceiling(remaining)));
		var pursuitSpeedMultiplier = EngagementQueries.PursuitSpeedMultiplier(actor.State, intent);
		var sampleCache = new Dictionary<int, (PursuitCourse? Course, double TravelTicks, double FutureTicks)>();

		(PursuitCourse? Course, double TravelTicks, double FutureTicks) EvaluateSample(int sampleIndex)
		{
			if (sampleCache.TryGetValue(sampleIndex, out var cached))
				return cached;

			var futureTicks = remaining * sampleIndex / sampleCount;
			var destination = targetPath
				.SampleAtElapsed(elapsed + futureTicks, target.State.SpeedPerTick)
				.Position;
			var course = FindCourse(origin, destination);

			var travelTicks = course is null
				? double.PositiveInfinity
				: course.Path
					.WithSpeedMultiplier(pursuitSpeedMultiplier)
					.TicksRequired(actor.State.SpeedPerTick);
			var evaluated = (course, travelTicks, futureTicks);
			sampleCache[sampleIndex] = evaluated;
			return evaluated;
		}

		var lo = 1;
		var hi = sampleCount;
		var firstFeasible = -1;
		while (lo <= hi)
		{
			var mid = (lo + hi) / 2;
			var (_, travelTicks, futureTicks) = EvaluateSample(mid);
			if (travelTicks <= futureTicks)
			{
				firstFeasible = mid;
				hi = mid - 1;
			}
			else
				lo = mid + 1;
		}

		if (firstFeasible >= 0)
			return sampleCache[firstFeasible].Course;

		PursuitCourse? finalCourse = null;
		for (var sampleIndex = sampleCount; sampleIndex >= 1; sampleIndex--)
		{
			var (course, _, _) = EvaluateSample(sampleIndex);
			if (course is not null)
			{
				finalCourse = course;
				break;
			}
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
		Coord interceptDestination)
	{
		if (!state.TravelTarget.MatchesFleet(targetFleetId, EContactIntent.Engagement)
			|| state.Travel is not FleetTravel.Journey journey
			|| runtime.CachedPath is null)
			return false;

		return journey.Destination == interceptDestination;
	}

	internal static void RecordReplan(
		ActorRuntime runtime,
		int currentTick,
		State target,
		Coord interceptDestination)
	{
		if (target.Travel is FleetTravel.Journey journey)
		{
			runtime.PursuitTrackedTargetJourneyId = journey.Id;
			runtime.PursuitTrackedTargetJourneyDestination = journey.Destination;
		}
		else if (target.Travel is FleetTravel.AtRest atRest)
		{
			runtime.PursuitTrackedTargetJourneyId = -1;
			runtime.PursuitTrackedTargetJourneyDestination = atRest.Position;
		}
		else
		{
			runtime.PursuitTrackedTargetJourneyId = -1;
			runtime.PursuitTrackedTargetJourneyDestination = default;
		}

		runtime.PursuitLastReplanTick = currentTick;
		runtime.PursuitPlannedInterceptDestination = interceptDestination;
		runtime.PursuitForceReplan = false;
	}

	internal static void ClearReplan(ActorRuntime runtime)
	{
		runtime.PursuitLastReplanTick = 0;
		runtime.PursuitTrackedTargetJourneyId = -1;
		runtime.PursuitTrackedTargetJourneyDestination = default;
		runtime.PursuitPlannedInterceptDestination = default;
		runtime.PursuitForceReplan = false;
	}

	internal static void NotifyFleetMoved(
		StarMap world,
		Func<string, ActorRuntime> runtimeFor,
		string movedFleetId)
	{
		foreach (var fleet in world.FleetRegistry.All)
		{
			if (!IsPursuingFleet(fleet.State, movedFleetId))
				continue;

			runtimeFor(fleet.State.Id).PursuitForceReplan = true;
		}
	}

	private static bool IsPursuingFleet(State hunter, string targetFleetId) =>
		hunter.PursuitDirective?.TargetFleetId == targetFleetId
		|| string.Equals(
			hunter.CurrentEngagement?.Hunting,
			targetFleetId,
			StringComparison.Ordinal)
		|| hunter.TravelTarget.MatchesFleet(targetFleetId, EContactIntent.Engagement);

	private static bool ShouldDeferReplan(
		StarMap world,
		State hunter,
		State target,
		ActorRuntime runtime,
		string targetFleetId,
		Coord targetPosition)
	{
		if (!hunter.TravelTarget.MatchesFleet(targetFleetId, EContactIntent.Engagement)
			|| hunter.Travel is not FleetTravel.Journey journey
			|| runtime.CachedPath is null)
			return false;

		if (runtime.PursuitForceReplan || TargetCourseChanged(target, runtime))
			return false;

		if (target.Travel is FleetTravel.Journey)
			return journey.Destination == runtime.PursuitPlannedInterceptDestination;

		if (journey.Destination == targetPosition)
			return true;

		var ticksSinceReplan = world.Timeline.Clock.Current - runtime.PursuitLastReplanTick;
		var hunterPosition = hunter.PositionAt(world, runtime.CachedPath, 0).Position;
		var maxClosingSpeed = EngagementQueries.MaximumTravelSpeed(hunter)
			+ EngagementQueries.MaximumTravelSpeed(target);
		var delay = EngagementQueries.ContactCheckDelay(
			hunterPosition,
			targetPosition,
			hunter.EngageRadius,
			maxClosingSpeed);
		return ticksSinceReplan < delay;
	}

	private static bool TargetCourseChanged(State target, ActorRuntime runtime)
	{
		if (target.Travel is FleetTravel.Journey journey)
			return journey.Id != runtime.PursuitTrackedTargetJourneyId
				|| journey.Destination != runtime.PursuitTrackedTargetJourneyDestination;

		if (target.Travel is FleetTravel.AtRest atRest)
		{
			if (runtime.PursuitTrackedTargetJourneyId != -1)
				return true;

			return atRest.Position != runtime.PursuitTrackedTargetJourneyDestination;
		}

		return runtime.PursuitTrackedTargetJourneyId != -1;
	}
}
