using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.FleetSpawner;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Agents;

public sealed class FleetSpawnerExecutionAgent : ExecutionAgent<StarMap, ActorRuntime>
{
	private readonly Func<StarMap> _world;
	private readonly Func<string, ActorRuntime> _runtimeFor;
	private readonly Func<bool> _generationEnabled;
	private readonly FleetSpawnerConfig _config;

	public FleetSpawnerExecutionAgent(
		Func<StarMap> world,
		Func<string, ActorRuntime> runtimeFor,
		Func<bool> generationEnabled,
		FleetSpawnerConfig? config = null)
	{
		_world = world;
		_runtimeFor = runtimeFor;
		_generationEnabled = generationEnabled;
		_config = config ?? new FleetSpawnerConfig();
	}

	public void PlanAndPublish()
	{
		if (!_canWork || _actorId is null)
			return;

		ClearBatchInFlight();
		var map = _world();
		var tick = map.Timeline.Clock.Current;
		var additions = new List<FleetSpawnerAddition>();
		if (_generationEnabled() && _config.CadenceTicks > 0 && tick % _config.CadenceTicks == 0)
		{
			var slots = System.Math.Max(
				0,
				_config.RandomAreaTargetCount
				- FleetSpawnerQueries.CountTowardTarget(
					map.FleetRegistry,
					EFleetSpawnerSource.RandomArea,
					tick));
			var reserved = new HashSet<Coord>();
			for (var slot = 0; slot < slots; slot++)
			{
				if (!RandomAreaFleetSpawnRecipe.TryCreate(
					map,
					tick,
					slot,
					reserved,
					tick + _config.TtlTicks,
					out var fleet))
					continue;

				reserved.Add(fleet.State.IdleCoord);
				additions.Add(new FleetSpawnerAddition(fleet));
			}
		}

		var action = new MaintainFleetSpawnerAction(StarSystemActorIds.FleetSpawner, tick, additions);
		if (MaintainFleetSpawnerDef.Instance.IsLegal(action, map, _runtimeFor(_actorId)))
			Publish([action]);
	}
}
