using GrimSpace.Core.Engine;
using GrimSpace.Math;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Events;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Agents;

/// <summary>
/// Wakes on a fixed cadence and tops the timeline up to a target number of future
/// world-event rolls. It only decides when to schedule and which event to start.
/// </summary>
public sealed class WorldEventScheduleExecutionAgent : ExecutionAgent<StarMap, ActorRuntime>
{
	private readonly Func<StarMap> _world;
	private readonly Func<string, ActorRuntime> _runtimeFor;

	public WorldEventScheduleExecutionAgent(
		Func<StarMap> world,
		Func<string, ActorRuntime> runtimeFor)
	{
		_world = world;
		_runtimeFor = runtimeFor;
	}

	public void PlanAndPublish()
	{
		if (!_canWork || _actorId is null)
			return;

		ClearBatchInFlight();
		var map = _world();
		var tick = map.Timeline.Clock.Current;
		if (tick <= 0 || tick % WorldEventConfig.WakeCadenceTicks != 0)
			return;

		var missing = WorldEventConfig.TargetScheduledRolls
			- map.Timeline.CountPending(pending => pending is IWorldEventRoll);
		if (missing <= 0)
			return;

		var slots = Enumerable.Range(0, missing)
			.Select(slot => PlanSlot(map.Seed, tick, slot))
			.ToArray();
		var action = new MaintainWorldEventScheduleAction(
			StarSystemActorIds.WorldEvents,
			tick,
			slots);
		if (!MaintainWorldEventScheduleDef.Instance.IsLegal(action, map, _runtimeFor(_actorId)))
			return;

		Publish([action]);
	}

	private static WorldEventSlot PlanSlot(int mapSeed, int tick, int slot)
	{
		var rng = new StableRandom(
			StableSeedMixer.From(mapSeed).Add(tick).Add(slot).Add("world-event-slot").Value);
		var eventId = WorldEventCatalog.Pick(rng.NextDouble());
		var delay = 1 + (int)(rng.NextDouble() * WorldEventConfig.WakeCadenceTicks);
		var rollSeed = StableSeedMixer.From(mapSeed).Add(tick).Add(slot).Add("world-event-roll").Value;
		return new WorldEventSlot(eventId, delay, rollSeed);
	}
}
