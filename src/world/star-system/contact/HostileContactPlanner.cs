using GrimSpace.Core.Log;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Contact;

public sealed class HostileContactPlanner
{
	private readonly Func<StarMap> _world;
	private readonly Func<string, ActorRuntime> _runtimeFor;
	private readonly PursuitPlanner _pursuitPlanner;

	public HostileContactPlanner(
		Func<StarMap> world,
		Func<string, ActorRuntime> runtimeFor,
		PursuitPlanner pursuitPlanner)
	{
		_world = world;
		_runtimeFor = runtimeFor;
		_pursuitPlanner = pursuitPlanner;
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

		foreach (var target in candidates)
		{
			var course = _pursuitPlanner.PlanInterceptCourse(
				actorId,
				target.State.Id,
				EContactIntent.Engagement);
			if (course is null)
				GameLog.Log(
					$"[star-map] hostile skip actor={actorId} target={target.State.Id} reason=no_path");
			else
			{
				var reaction = new ReactToHostileContactAction(
					actorId,
					new FleetContactTarget(target.State.Id),
					course.Destination,
					course.Path);
				if (ReactToHostileContactDef.Instance.IsLegal(reaction, world, runtime))
				{
					GameLog.Log(
						$"[star-map] hostile found actor={actorId} target={target.State.Id} " +
						$"aggression={actor.State.AggressionRating} destination={course.Destination}");
					return reaction;
				}
			}
		}

		return null;
	}
}
