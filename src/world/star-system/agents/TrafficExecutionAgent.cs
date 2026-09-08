using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Agents;

public sealed class TrafficExecutionAgent : ExecutionAgent<StarMap, ActorRuntime>
{
	private readonly Func<StarMap> _world;
	private readonly Func<string, ActorRuntime> _runtimeFor;
	private readonly IPathfinder _pathfinder;

	public TrafficExecutionAgent(
		Func<StarMap> world,
		Func<string, ActorRuntime> runtimeFor,
		IPathfinder pathfinder)
	{
		_world = world;
		_runtimeFor = runtimeFor;
		_pathfinder = pathfinder;
	}

	public void PlanAndPublish()
	{
		if (!_canWork || _actorId is null)
			return;

		ClearBatchInFlight();
		var actions = PlanFromLiveState();
		if (actions.Count == 0)
			return;

		Publish(actions);
	}

	private IReadOnlyList<IAction> PlanFromLiveState()
	{
		var unitId = _actorId!;
		var world = _world();
		var runtime = _runtimeFor(unitId);
		var state = world.UnitRegistry.UnitOf(unitId).State;

		if (state.ChoreDockIds.Count == 0
			|| state.Phase != EPhase.Docked
			|| string.IsNullOrEmpty(state.DockedAtDockId))
			return [];

		var destinationDockId = state.NextChoreDockId();
		var origin = world.DocksById[state.DockedAtDockId].Position;
		var destination = world.DocksById[destinationDockId].Position;
		var result = _pathfinder.FindPath(origin, destination);
		if (result is not PathfindingResult.Found found)
			return [];

		var move = new MoveAction(unitId, unitId, destination, found.Path);
		return MoveDef.Instance.IsLegal(move, world, runtime) ? [move] : [];
	}
}
