using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Presentation;
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

	internal bool IsCommittedForDiagnostics => _committed;

	internal bool CanWorkForDiagnostics => _canWork;

	public event Action? PlanningChanged;

	public CourseCommandResult TryQueueMove(Coord destination)
	{
		if (_committed || !_canWork || _actorId is null)
		{
			StarMapPresentationDiagnostics.LogMoveQueueFailed(
				ActionLegalityDiagnostics.DescribeAgentBlocked(this),
				destination,
				this);
			return new CourseCommandResult.Unreachable();
		}

		var anchorWorld = _anchorWorld();
		var unit = anchorWorld.UnitRegistry.UnitOf(_actorId);
		var (origin, _) = unit.State.CommittedPosition(
			anchorWorld,
			_runtimeFor(_actorId).CachedPath,
			0f);
		var result = _pathfinder.FindPath(origin, destination);
		if (result is not PathfindingResult.Found found)
		{
			StarMapPresentationDiagnostics.LogMoveQueueFailed("no_path", destination, this);
			return new CourseCommandResult.Unreachable();
		}

		if (TryEnqueue([new MoveAction(_actorId, _actorId, destination, found.Path)]))
		{
			StarMapPresentationDiagnostics.LogCourseQueued("move", destination, this);
			return new CourseCommandResult.Queued(found.Path);
		}

		return new CourseCommandResult.Unreachable();
	}

	public CourseCommandResult TryQueueHuntUnit(string targetUnitId)
	{
		if (_committed || !_canWork || _actorId is null)
		{
			StarMapPresentationDiagnostics.LogMoveQueueFailed(
				ActionLegalityDiagnostics.DescribeAgentBlocked(this),
				null,
				this);
			return new CourseCommandResult.Unreachable();
		}

		var anchorWorld = _anchorWorld();
		if (!anchorWorld.UnitRegistry.TryGet(_actorId, out var initiator)
			|| !anchorWorld.UnitRegistry.TryGet(targetUnitId, out var target)
			|| target.State.CombatProfile is null
			|| target.State.Type == EType.PlayerFleet)
		{
			StarMapPresentationDiagnostics.LogMoveQueueFailed("invalid_hunt_target", null, this);
			return new CourseCommandResult.Unreachable();
		}

		var origin = _committedPositionOf(_actorId);
		var destination = _committedPositionOf(targetUnitId);
		var result = _pathfinder.FindPath(origin, destination);
		if (result is not PathfindingResult.Found found)
		{
			StarMapPresentationDiagnostics.LogMoveQueueFailed("no_path", destination, this);
			return new CourseCommandResult.Unreachable();
		}

		if (TryEnqueue([new HuntUnitAction(_actorId, targetUnitId, destination, found.Path)]))
		{
			StarMapPresentationDiagnostics.LogCourseQueued("hunt", destination, this);
			return new CourseCommandResult.Queued(found.Path);
		}

		return new CourseCommandResult.Unreachable();
	}

	public bool TryEnqueue(IReadOnlyList<IAction> actions)
	{
		if (actions.Count != 1)
		{
			if (actions.Count > 0)
				StarMapPresentationDiagnostics.LogActionRejected(actions[0], "invalid_batch", this);
			return false;
		}

		var action = actions[0];
		if (_committed || !_canWork)
		{
			StarMapPresentationDiagnostics.LogActionRejected(
				action,
				ActionLegalityDiagnostics.DescribeAgentBlocked(this),
				this);
			return false;
		}

		Sim = _createSimulation();
		if (!Sim.TryEnqueue(action))
		{
			var reason = ActionLegalityDiagnostics.DescribeIllegality(
				action,
				Sim.World,
				Sim.RuntimeFor(action.ActorId));
			StarMapPresentationDiagnostics.LogActionRejected(action, reason, this);
			return false;
		}

		_pendingAction = action;
		NotifyPlanningChanged();
		StarMapPresentationDiagnostics.LogActionQueued(action, this);
		return true;
	}

	public bool Undo() => false;

	public bool Commit()
	{
		if (_pendingAction is null)
			return false;

		if (_committed || !_canWork)
		{
			StarMapPresentationDiagnostics.LogCommitSkipped(
				_committed ? "already_committed" : "agent_not_working",
				this);
			return false;
		}

		var action = _pendingAction;
		_pendingAction = null;
		_committed = true;
		Publish([action]);
		NotifyPlanningChanged();
		StarMapPresentationDiagnostics.LogActionCommitted(action, this);
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
