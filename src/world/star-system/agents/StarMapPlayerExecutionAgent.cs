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
	private MoveAction? _pendingMove;

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

	public MoveAction? PendingMove => _pendingMove;

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

		_pendingMove = new MoveAction(_actorId, _actorId, destination, found.Path);
		RebuildPreview();
		return new MoveCommandResult.Queued(found.Path);
	}

	public bool TryEnqueue(IReadOnlyList<IAction> actions)
	{
		if (_committed || !_isActive || actions.Count != 1 || actions[0] is not MoveAction move)
			return false;

		_pendingMove = move;
		RebuildPreview();
		return true;
	}

	public bool Undo() => false;

	public bool Commit()
	{
		if (_committed || !_isActive)
			return false;

		_committed = true;
		Complete(_pendingMove is null ? [] : [_pendingMove]);
		NotifyPlanningChanged();
		return true;
	}

	protected override void ProduceActionsJob(Simulation<StarMap, ActorRuntime> simulation)
	{
		_committed = false;
		_pendingMove = null;
		Sim = simulation;
		NotifyPlanningChanged();
	}

	private void RebuildPreview()
	{
		Sim = _createSimulation();
		if (_pendingMove is not null)
			Sim.TryEnqueue(_pendingMove);

		NotifyPlanningChanged();
	}

	private void NotifyPlanningChanged() => PlanningChanged?.Invoke();
}
