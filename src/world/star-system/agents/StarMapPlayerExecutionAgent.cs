using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Agents;

public sealed class StarMapPlayerExecutionAgent
	: SimulationExecutionAgent<StarMap, ActorRuntime>,
		IActionSink
{
	private readonly Func<Simulation<StarMap, ActorRuntime>> _createSimulation;
	private readonly Func<StarMap> _anchorWorld;
	private readonly Func<string, ActorRuntime> _runtimeFor;
	private readonly Func<string, Coord> _committedPositionOf;
	private readonly IPathfinder _pathfinder;
	private bool _committed;
	private IAction? _pendingAction;

	public StarMapPlayerExecutionAgent(
		Func<Simulation<StarMap, ActorRuntime>> createSimulation,
		Func<StarMap> anchorWorld,
		Func<string, ActorRuntime> runtimeFor,
		Func<string, Coord> committedPositionOf,
		IPathfinder pathfinder)
	{
		_createSimulation = createSimulation;
		_anchorWorld = anchorWorld;
		_runtimeFor = runtimeFor;
		_committedPositionOf = committedPositionOf;
		_pathfinder = pathfinder;
	}

	public new Simulation<StarMap, ActorRuntime> Sim { get; private set; } = null!;

	public bool HasPendingAction => _pendingAction is not null;

	public MoveAction? PendingMove => _pendingAction as MoveAction;

	public PendingCourse? PendingCourse => _pendingAction switch
	{
		MoveAction move => new PendingCourse(move.Destination, move.Path),
		HuntUnitAction hunt => new PendingCourse(hunt.Destination, hunt.Path),
		_ => null,
	};

	public bool IsPlanning => _canWork && !_committed;

	public bool CanUndo => false;

	public event Action? PlanningChanged;

	public CourseCommandResult TryQueueMove(Coord destination)
	{
		if (_committed || !_canWork || _actorId is null)
			return new CourseCommandResult.Unreachable();

		var anchorWorld = _anchorWorld();
		var unit = anchorWorld.UnitRegistry.UnitOf(_actorId);
		var (origin, _) = unit.State.CommittedPosition(
			anchorWorld,
			_runtimeFor(_actorId).CachedPath,
			0f);
		var result = _pathfinder.FindPath(origin, destination);
		if (result is not PathfindingResult.Found found)
			return new CourseCommandResult.Unreachable();

		return TryEnqueue([new MoveAction(_actorId, _actorId, destination, found.Path)])
			? new CourseCommandResult.Queued(found.Path)
			: new CourseCommandResult.Unreachable();
	}

	public CourseCommandResult TryQueueHuntUnit(string targetUnitId)
	{
		if (_committed || !_canWork || _actorId is null)
			return new CourseCommandResult.Unreachable();

		var anchorWorld = _anchorWorld();
		if (!anchorWorld.UnitRegistry.TryGet(_actorId, out var initiator)
			|| !anchorWorld.UnitRegistry.TryGet(targetUnitId, out var target)
			|| target.State.CombatProfile is null
			|| target.State.Type == EType.PlayerFleet)
			return new CourseCommandResult.Unreachable();

		var origin = _committedPositionOf(_actorId);
		var destination = _committedPositionOf(targetUnitId);
		var result = _pathfinder.FindPath(origin, destination);
		if (result is not PathfindingResult.Found found)
			return new CourseCommandResult.Unreachable();

		return TryEnqueue([new HuntUnitAction(_actorId, targetUnitId, destination, found.Path)])
			? new CourseCommandResult.Queued(found.Path)
			: new CourseCommandResult.Unreachable();
	}

	public bool TryEnqueue(IReadOnlyList<IAction> actions)
	{
		if (_committed || !_canWork || actions.Count != 1)
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
		if (_committed || !_canWork || _pendingAction is null)
			return false;

		var action = _pendingAction;
		_pendingAction = null;
		_committed = true;
		Publish([action]);
		NotifyPlanningChanged();
		return true;
	}

	protected override bool PublishOnActivate => false;

	protected override void ProduceActionsJob(Simulation<StarMap, ActorRuntime> simulation)
	{
		_committed = false;
		_pendingAction = null;
		Sim = simulation;
		NotifyPlanningChanged();
	}

	private void NotifyPlanningChanged() => PlanningChanged?.Invoke();
}
