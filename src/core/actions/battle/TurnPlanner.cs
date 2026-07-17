using GrimSpace.Battle.Board;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Contexts;
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
	private readonly PlanQueue<IAction> _actions = new();
	private readonly TurnState _turnState = new();
	private string? _ownerId;
	private IReadOnlyList<Unit>? _roster;
	private BoundedGrid? _grid;
	private IReadOnlySet<Coord>? _blockedCells;
	private IReadOnlyDictionary<string, NonUnit>? _nonUnits;
	private BattleBoard? _board;

	public IReadOnlyList<IAction> Actions => _actions.Actions;

	public BattleBoard Board =>
		_board ?? throw new InvalidOperationException("Call BeginTurn before planning.");

	public BattlePlanContext Context => new(Actions, _turnState);

	public string? OwnerId => _ownerId;

	public void BeginTurn(
		string ownerId,
		IReadOnlyList<Unit> roster,
		BoundedGrid grid,
		IReadOnlyDictionary<string, NonUnit> nonUnits,
		IReadOnlySet<Coord> blockedCells)
	{
		_ownerId = ownerId;
		_roster = roster;
		_grid = grid;
		_nonUnits = nonUnits;
		_blockedCells = blockedCells;

		_actions.Clear();
		_turnState.Clear();
		_board = BattleBoard.FromSnapshot(roster, nonUnits, grid, blockedCells);
	}

	public void CopyFrom(IEnumerable<IAction> actions)
	{
		_actions.Clear();
		_turnState.Clear();
		_ownerId = null;
		_roster = null;
		_grid = null;
		_blockedCells = null;
		_nonUnits = null;
		_board = null;

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

	public bool TryUndoLast()
	{
		if (!_actions.TryPopLast(out _))
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

	public static bool TryApplyAll(
		IReadOnlyList<IAction> actions,
		BattleBoard board,
		BattlePlanContext context,
		string actorId)
	{
		context.TurnState.Clear();

		foreach (var action in actions)
		{
			if (!TryApplyOne(action, board, context, actorId))
				return false;
		}

		return true;
	}

	public static bool TryApplyOne(
		IAction action,
		BattleBoard board,
		BattlePlanContext context,
		string actorId,
		bool checkLegal = true)
	{
		if (checkLegal && !action.IsLegal(board, context))
			return false;

		var slices = BattleSlices.For(board, actorId, context.TurnState);
		foreach (var effect in action.Resolve(board, context))
			effect.Apply(slices);

		return true;
	}

	public static void RunPhaseEnd(State actor, IReadOnlyList<IAction> plan)
	{
		if (!plan.Any(action => action is MoveAction))
			actor.MomentumLevel = System.Math.Max(actor.MomentumLevel - 1, 0);
	}

	public static void RunPhaseEnd(BattleBoard board, string actorId, IReadOnlyList<IAction> plan) =>
		RunPhaseEnd(board.StateOf(actorId), plan);

	public static BattleBoard BuildPreviewBoard(
		IReadOnlyList<Unit> roster,
		BoundedGrid grid,
		IReadOnlyList<IAction> actions,
		IReadOnlySet<Coord> blockedCells,
		string actorId,
		IReadOnlyDictionary<string, NonUnit>? nonUnits = null)
	{
		var board = BattleBoard.FromSnapshot(roster, nonUnits ?? new Dictionary<string, NonUnit>(), grid, blockedCells);
		var turnState = new TurnState();
		var context = new BattlePlanContext(actions, turnState);
		TryApplyAll(actions, board, context, actorId);
		return board;
	}

	public static void ApplyToLive(
		IReadOnlyList<IAction> actions,
		IReadOnlyList<Unit> roster,
		BoundedGrid grid,
		IDictionary<string, NonUnit> nonUnits,
		IReadOnlySet<Coord> blockedCells,
		string? actorId = null)
	{
		var board = BattleBoard.FromLive(roster, nonUnits, grid, blockedCells);
		var turnState = new TurnState();
		var context = new BattlePlanContext(actions, turnState);
		TryApplyAll(actions, board, context, actorId!);

		if (actorId is not null)
			RunPhaseEnd(board, actorId, actions);
	}

	public static void ApplyCommittedAction(
		IAction action,
		IReadOnlyList<Unit> roster,
		BoundedGrid grid,
		IDictionary<string, NonUnit> nonUnits,
		IReadOnlySet<Coord> blockedCells,
		BattlePlanContext context,
		string actorId)
	{
		var board = BattleBoard.FromLive(roster, nonUnits, grid, blockedCells);
		TryApplyOne(action, board, context, actorId, checkLegal: false);
	}

	private void Replay()
	{
		EnsureTurnContext();
		_board = BattleBoard.FromSnapshot(_roster!, _nonUnits!, _grid!, _blockedCells!);
		TryApplyAll(Actions, _board, Context, _ownerId!);
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
