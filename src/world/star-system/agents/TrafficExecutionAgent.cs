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
	private readonly IPathfinder _pathfinder;

	public TrafficExecutionAgent(IPathfinder pathfinder) => _pathfinder = pathfinder;

	protected override void ProduceActionsJob(Simulation<StarMap, ActorRuntime> simulation)
	{
		var unitId = _actorId!;
		var world = simulation.World;
		var unit = world.UnitRegistry.UnitOf(unitId);
		var state = unit.State;

		if (state.ChoreDockIds.Count == 0 || !state.IsReadyToDepart)
		{
			Complete([]);
			return;
		}

		var destinationDockId = state.NextChoreDockId();
		var origin = world.DocksById[state.DockedAtDockId].Position;
		var destination = world.DocksById[destinationDockId].Position;
		var result = _pathfinder.FindPath(origin, destination);
		if (result is not PathfindingResult.Found found)
		{
			Complete([]);
			return;
		}

		Complete([new MoveAction(unitId, unitId, destination, found.Path)]);
	}
}
