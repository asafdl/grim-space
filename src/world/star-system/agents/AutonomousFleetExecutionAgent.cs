using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Agents;

public sealed class AutonomousFleetExecutionAgent : ExecutionAgent<StarMap, ActorRuntime>
{
	private readonly Func<StarMap> _world;
	private readonly Func<string, ActorRuntime> _runtimeFor;
	private readonly PursuitPlanner _pursuitPlanner;
	private readonly HostileContactPlanner _hostileContactPlanner;
	private readonly PatrolPlanner _patrolPlanner;

	public AutonomousFleetExecutionAgent(
		Func<StarMap> world,
		Func<string, ActorRuntime> runtimeFor,
		IPathfinder pathfinder)
	{
		_world = world;
		_runtimeFor = runtimeFor;
		_pursuitPlanner = new PursuitPlanner(world, runtimeFor, pathfinder);
		_hostileContactPlanner = new HostileContactPlanner(world, runtimeFor, pathfinder);
		_patrolPlanner = new PatrolPlanner(world, runtimeFor, pathfinder);
	}

	public void PlanAndPublish()
	{
		if (!_canWork || _actorId is null)
			return;

		ClearBatchInFlight();
		var world = _world();
		var state = world.FleetRegistry.FleetOf(_actorId).State;
		var runtime = _runtimeFor(_actorId);
		if (state.CurrentEngagement is { Phase: not Units.EEngagementPhase.Pursuing }
			|| runtime.ActionCooldownUntilTick > world.Timeline.Clock.Current)
			return;

		var pursuit = _pursuitPlanner.Plan(_actorId);
		if (pursuit is not null)
		{
			Publish([pursuit]);
			return;
		}

		var hostileReaction = _hostileContactPlanner.Plan(_actorId);
		if (hostileReaction is not null)
		{
			Publish([hostileReaction]);
			return;
		}

		var patrol = _patrolPlanner.Plan(_actorId);
		if (patrol is not null)
			Publish([patrol]);
	}
}
