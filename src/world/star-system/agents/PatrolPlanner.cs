using System.Collections.Immutable;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Agents;

public sealed class PatrolPlanner
{
	private const int MinimumJourneyTicks = 2;
	private const int WaypointsPerRoute = 4;

	private readonly Func<StarMap> _world;
	private readonly Func<string, ActorRuntime> _runtimeFor;
	private readonly IPathfinder _pathfinder;

	public PatrolPlanner(
		Func<StarMap> world,
		Func<string, ActorRuntime> runtimeFor,
		IPathfinder pathfinder)
	{
		_world = world;
		_runtimeFor = runtimeFor;
		_pathfinder = pathfinder;
	}

	public MoveAction? Plan(string actorId)
	{
		var world = _world();
		var runtime = _runtimeFor(actorId);
		var fleet = world.FleetRegistry.FleetOf(actorId);
		var state = fleet.State;

		if (state.PatrolRadius <= 0
			|| state.Travel is not FleetTravel.AtRest { Position: var routeStart })
			return null;

		var legStart = routeStart;
		var routeLegs = new List<TransitLeg>();
		for (var waypointIndex = 0; waypointIndex < WaypointsPerRoute; waypointIndex++)
		{
			var leg = PlanLeg(
				state,
				world,
				legStart,
				world.Timeline.Clock.Current + waypointIndex);
			if (leg is null)
				return null;

			routeLegs.AddRange(leg.Value.Path.Legs);
			legStart = leg.Value.Destination;
		}

		var route = new TransitPath(routeLegs.ToImmutableArray());
		var move = new MoveAction(actorId, actorId, legStart, route);
		return MoveDef.Instance.IsLegal(move, world, runtime) ? move : null;
	}

	private (Coord Destination, TransitPath Path)? PlanLeg(
		State state,
		StarMap world,
		Coord origin,
		int selectionTick)
	{
		foreach (var destination in PatrolDestinations(state, world, origin, selectionTick))
		{
			if (_pathfinder.FindPath(origin, destination)
				is not PathfindingResult.Found found)
				continue;
			if (found.Path.DurationTicks(state.SpeedPerTick) < MinimumJourneyTicks)
				continue;

			return (destination, found.Path);
		}

		return null;
	}

	internal static IEnumerable<Coord> PatrolDestinations(
		State state,
		StarMap world,
		Coord current,
		int tick)
	{
		var radius = state.PatrolRadius;
		var candidates = Coord.OffsetsInCube(radius)
			.Where(offset =>
				offset.Y == 0
				&& offset.X * offset.X + offset.Z * offset.Z <= radius * radius)
			.Select(offset => state.PatrolOrigin + offset)
			.Where(destination =>
			{
				var displacement = destination - current;
				return 16 * (displacement.X * displacement.X + displacement.Z * displacement.Z)
					>= 9 * radius * radius;
			})
			.Where(destination => !world.DocksByPosition.ContainsKey(destination))
			.ToArray();
		if (candidates.Length == 0)
			yield break;

		var hash = 17;
		foreach (var character in state.Id)
			hash = unchecked(hash * 31 + character);

		var start = (int)(unchecked((uint)(hash + tick)) % (uint)candidates.Length);
		for (var i = 0; i < candidates.Length; i++)
			yield return candidates[(start + i) % candidates.Length];
	}
}
