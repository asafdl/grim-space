using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Contexts;
using GrimSpace.Core.Actions.Battle.Effects;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Core.Actions.Battle;

public sealed class SimulatedTurn
{
	public required BattleBoard Board { get; init; }
	public required string ActorId { get; init; }

	public State Actor => Board.StateOf(ActorId);

	public IEnumerable<Hazard> Hazards => Board.TurnHazards;
}

/// <summary>
/// Per-turn planning session: queue, preview board, turn state, replay, and action execution.
/// </summary>
public sealed class TurnPlanner
{
	private readonly PlanQueue _actions = new();
	private readonly TurnState _turnState = new();
	private readonly Timeline _previewTimeline = new();
	private readonly List<IAction> _appliedActions = [];
	private string? _ownerId;
	private IReadOnlyList<Unit>? _roster;
	private BoundedGrid? _grid;
	private IReadOnlySet<Coord>? _blockedCells;
	private IReadOnlyDictionary<string, NonUnit>? _nonUnits;
	private BattleBoard? _board;
	private int _turnStartTick;

	public IReadOnlyList<IAction> Actions => _actions.Actions;

	public IReadOnlyTimeline FutureSchedule => _previewTimeline;

	public int TurnStartTick => _turnStartTick;

	public BattleBoard Board =>
		_board ?? throw new InvalidOperationException("Call BeginTurn before planning.");

	public BattlePlanContext Context => new(_appliedActions, _turnState);

	public string? OwnerId => _ownerId;

	public void BeginTurn(
		string ownerId,
		IReadOnlyList<Unit> roster,
		BoundedGrid grid,
		IReadOnlyDictionary<string, NonUnit> nonUnits,
		IReadOnlySet<Coord> blockedCells,
		int turnStartTick)
	{
		_ownerId = ownerId;
		_roster = roster;
		_grid = grid;
		_nonUnits = nonUnits;
		_blockedCells = blockedCells;
		_turnStartTick = turnStartTick;

		_actions.Clear();
		_turnState.Clear();
		_appliedActions.Clear();
		_previewTimeline.ResetPreviewFork(turnStartTick);
		_board = BattleBoard.FromSnapshot(roster, nonUnits, grid, blockedCells);
	}

	public void CopyFrom(IEnumerable<IAction> actions)
	{
		_actions.Clear();
		_turnState.Clear();
		_appliedActions.Clear();
		_ownerId = null;
		_roster = null;
		_grid = null;
		_blockedCells = null;
		_nonUnits = null;
		_board = null;
		_turnStartTick = 0;

		foreach (var action in actions)
			_actions.Enqueue(action);
	}

	public bool TryApplyAndEnqueue(IAction action)
	{
		EnsureBoard();
		if (!action.IsLegal(Board, Context))
			return false;

		_actions.Enqueue(action);
		Replay();
		return true;
	}

	public void ForceApplyAndEnqueue(IAction action)
	{
		EnsureBoard();
		_actions.Enqueue(action);
		Replay();
	}

	public void EnqueueMovePath(string actorId, Option option)
	{
		EnsureBoard();
		var origin = Board.StateOf(actorId).Position;
		var frame = BodyFrame.From(Board.StateOf(actorId));
		var steps = MoveStepAction.BuildSteps(actorId, frame, origin, option.Path)
			.Cast<IAction>()
			.ToList();
		_actions.Enqueue(new PlanBatch(steps));
		Replay();
	}

	public bool TryEnqueueMovePath(string actorId, Option option)
	{
		if (_turnState.IsMovePathStarted)
			return false;

		EnqueueMovePath(actorId, option);
		return true;
	}

	public bool TryUndoLast()
	{
		if (!_actions.TryPopLastBatch(out _))
			return false;

		Replay();
		return true;
	}

	public SimulatedTurn GetPreview(string actorId) =>
		new()
		{
			Board = Board,
			ActorId = actorId,
		};

	public void AdvanceToTick(int tick)
	{
		EnsureTurnContext();

		for (var t = _turnStartTick + 1; t <= tick; t++)
		{
			_previewTimeline.Clock.Set(t);
			while (_previewTimeline.At(t).TryDequeue(out var scheduled) && scheduled is not null)
			{
				var applied = new List<IAction>();
				var context = new BattlePlanContext(applied, _turnState);
				TryApplyOne(scheduled, Board, context, _previewTimeline, _ownerId!, checkLegal: false);
			}
		}
	}

	public static bool TryApplyAll(
		IReadOnlyList<IAction> actions,
		BattleBoard board,
		BattlePlanContext context,
		Timeline timeline,
		string actorId)
	{
		context.TurnState.Clear();
		var applied = new List<IAction>();

		foreach (var action in actions)
		{
			var stepContext = new BattlePlanContext(applied, context.TurnState);
			if (!TryApplyOne(action, board, stepContext, timeline, actorId))
				return false;

			applied.Add(action);
		}

		return true;
	}

	public static bool TryApplyOne(
		IAction action,
		BattleBoard board,
		BattlePlanContext context,
		Timeline timeline,
		string actorId,
		bool checkLegal = true)
	{
		if (checkLegal && !action.IsLegal(board, context))
			return false;

		var slices = SystemAction.Is(actorId)
			? BattleSlices.ForSystem(board, timeline)
			: BattleSlices.For(board, actorId, context.TurnState, timeline);
		foreach (var effect in action.Resolve(board, context))
			effect.Apply(slices);

		return true;
	}

	public static void ApplyToLive(
		IReadOnlyList<IAction> actions,
		IReadOnlyList<Unit> roster,
		BoundedGrid grid,
		IDictionary<string, NonUnit> nonUnits,
		IReadOnlySet<Coord> blockedCells,
		Timeline timeline,
		string? actorId = null)
	{
		var board = BattleBoard.FromLive(roster, nonUnits, grid, blockedCells);
		var turnState = new TurnState();
		var applied = new List<IAction>();
		var context = new BattlePlanContext(applied, turnState);
		var phaseActions = WithPhaseEnd(actions, actorId);
		TryApplyAll(phaseActions, board, context, timeline, actorId!);
	}

	private static IReadOnlyList<IAction> WithPhaseEnd(IReadOnlyList<IAction> actions, string? actorId)
	{
		if (actorId is null)
			return actions;

		var phaseActions = new List<IAction>(actions) { new EndOfPhaseAction(actorId) };
		return phaseActions;
	}

	public static void ApplyCommittedAction(
		IAction action,
		IReadOnlyList<Unit> roster,
		BoundedGrid grid,
		IDictionary<string, NonUnit> nonUnits,
		IReadOnlySet<Coord> blockedCells,
		BattlePlanContext context,
		Timeline timeline,
		string actorId)
	{
		var board = BattleBoard.FromLive(roster, nonUnits, grid, blockedCells);
		TryApplyOne(action, board, context, timeline, actorId, checkLegal: false);
	}

	private void Replay()
	{
		EnsureTurnContext();
		_board = BattleBoard.FromSnapshot(_roster!, _nonUnits!, _grid!, _blockedCells!);
		_previewTimeline.ResetPreviewFork(_turnStartTick);
		_previewTimeline.Clock.Set(_turnStartTick);
		_turnState.Clear();
		_appliedActions.Clear();
		TryApplyAll(WithPhaseEnd(Actions, _ownerId), _board, Context, _previewTimeline, _ownerId!);
	}

	private void EnsureBoard()
	{
		if (_board is null)
			throw new InvalidOperationException("Call BeginTurn before planning.");
	}

	private void EnsureTurnContext()
	{
		if (_ownerId is null || _roster is null || _grid is null || _blockedCells is null || _nonUnits is null)
			throw new InvalidOperationException("Call BeginTurn before planning.");
	}
}
