using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Agents;

public sealed class TravelEconomyExecutionAgent : ExecutionAgent<StarMap, ActorRuntime>
{
	private readonly Func<StarMap> _world;
	private readonly Func<string, ActorRuntime> _runtimeFor;
	private readonly Func<string?> _playerId;

	public TravelEconomyExecutionAgent(
		Func<StarMap> world,
		Func<string, ActorRuntime> runtimeFor,
		Func<string?> playerId)
	{
		_world = world;
		_runtimeFor = runtimeFor;
		_playerId = playerId;
	}

	public void PlanAndPublish()
	{
		if (!_canWork || _actorId is null)
			return;

		ClearBatchInFlight();
		var playerId = _playerId();
		if (playerId is null)
			return;

		var map = _world();
		var playerRuntime = _runtimeFor(playerId);
		if (!map.FleetRegistry.TryGet(playerId, out var fleet)
			|| fleet.State.Travel is not FleetTravel.Journey
			|| playerRuntime.CachedPath is null)
			return;

		var action = new ChargeTravelFuelAction(playerId, map.Timeline.Clock.Current);
		if (!ChargeTravelFuelDef.Instance.IsLegal(action, map, playerRuntime))
			return;

		Publish([action]);
	}
}
