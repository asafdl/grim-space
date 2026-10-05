using GrimSpace.Math.Grid;
using GrimSpace.Core.Log;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.World.StarSystem.Vision;

namespace GrimSpace.World.StarSystem.Contact;

public sealed class HostileContactPlanner
{
	private readonly Func<StarMap> _world;
	private readonly Func<string, ActorRuntime> _runtimeFor;
	private readonly IPathfinder _pathfinder;

	public HostileContactPlanner(
		Func<StarMap> world,
		Func<string, ActorRuntime> runtimeFor,
		IPathfinder pathfinder)
	{
		_world = world;
		_runtimeFor = runtimeFor;
		_pathfinder = pathfinder;
	}

	public ReactToHostileContactAction? Plan(string actorId)
	{
		var world = _world();
		var runtime = _runtimeFor(actorId);
		if (!world.FleetRegistry.TryGet(actorId, out var actor))
			return null;

		if (actor.State.Type != EType.PirateFleet
			|| actor.State.AggressionRating == 0)
			return null;

		var candidates = HostileContactQueries.FindEligibleTargets(
			world,
			actorId,
			_runtimeFor,
			0f,
			world.Timeline.Clock.Current);
		var origin = MoveDef.ResolveOrigin(world, actor, runtime);

		foreach (var target in candidates)
		{
			var targetSample = FleetPositionSampler.Sample(
				world,
				target.State,
				_runtimeFor(target.State.Id),
				0f);
			var destination = new Coord(
				(int)System.Math.Round(targetSample.X),
				0,
				(int)System.Math.Round(targetSample.Z));
			if (_pathfinder.FindPath(origin, destination)
				is not PathfindingResult.Found found)
				GameLog.Log(
					$"[star-map] hostile skip actor={actorId} target={target.State.Id} reason=no_path");
			else
			{
				var reaction = new ReactToHostileContactAction(
					actorId,
					new FleetContactTarget(target.State.Id),
					destination,
					found.Path);
				if (ReactToHostileContactDef.Instance.IsLegal(reaction, world, runtime))
				{
					GameLog.Log(
						$"[star-map] hostile found actor={actorId} target={target.State.Id} " +
						$"aggression={actor.State.AggressionRating} destination={destination}");
					return reaction;
				}
			}
		}

		return null;
	}
}
