using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Agents;

public sealed class StarMapPlayerExecutionAgent
	: ExecutionAgent<StarMap, ActorRuntime>,
		IActionSink
{
	private readonly Func<Simulation<StarMap, ActorRuntime>> _createSimulation;
	private readonly Func<StarMap> _anchorWorld;
	private readonly IPathfinder _pathfinder;
	private bool _committed;
	private IAction? _pendingAction;

	public StarMapPlayerExecutionAgent(
		Func<Simulation<StarMap, ActorRuntime>> createSimulation,
		Func<StarMap> anchorWorld,
		IPathfinder pathfinder)
	{
		_createSimulation = createSimulation;
		_anchorWorld = anchorWorld;
		_pathfinder = pathfinder;
	}

	public Simulation<StarMap, ActorRuntime> Sim { get; private set; } = null!;

	public bool HasPendingAction => _pendingAction is not null;

	public MoveAction? PendingMove => _pendingAction as MoveAction;

	public bool IsPlanning => _isActive && !_committed;

	public bool CanUndo => false;

	public event Action? PlanningChanged;

	public MoveCommandResult TryQueueMove(Coord destination)
	{
		if (_committed || !_isActive || _actorId is null)
			return new MoveCommandResult.Unreachable();

		var anchorWorld = _anchorWorld();
		var unit = anchorWorld.UnitRegistry.UnitOf(_actorId);
		var (origin, _) = unit.State.CommittedPosition(
			anchorWorld,
			unit.Runtime.CachedPath,
			0f);
		var result = _pathfinder.FindPath(origin, destination);
		if (result is not PathfindingResult.Found found)
			return new MoveCommandResult.Unreachable();

		return TryEnqueue([new MoveAction(_actorId, _actorId, destination, found.Path)])
			? new MoveCommandResult.Queued(found.Path)
			: new MoveCommandResult.Unreachable();
	}

	public bool TryEnqueue(IReadOnlyList<IAction> actions)
	{
		if (_committed || !_isActive || actions.Count != 1)
			return false;

		Sim = _createSimulation();
		if (!Sim.TryEnqueue(actions[0]))
			return false;

		_pendingAction = actions[0];
		NotifyPlanningChanged();
		return true;
	}

	public bool Undo() => false;

	public bool Commit()
	{
		if (_committed || !_isActive || _pendingAction is null)
			return false;

		var action = _pendingAction;
		_pendingAction = null;
		_committed = true;
		Complete([action]);
		NotifyPlanningChanged();
		return true;
	}

	protected override void ProduceActionsJob(Simulation<StarMap, ActorRuntime> simulation)
	{
		_committed = false;
		_pendingAction = null;
		Sim = simulation;
		NotifyPlanningChanged();
	}

	private void NotifyPlanningChanged() => PlanningChanged?.Invoke();
}
