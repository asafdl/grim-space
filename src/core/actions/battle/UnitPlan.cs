using GrimSpace.Battle.Board;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Core.Actions.Battle;

/// <summary>
/// Per-unit turn plan: action queue plus the planning board for this turn.
/// </summary>
public sealed class UnitPlan
{
	private readonly PlanQueue<IAction> _actions = new();
	private readonly BattleTurnTags _tags = new();
	private string? _ownerId;
	private IReadOnlyList<Unit>? _roster;
	private BoundedGrid? _grid;
	private IReadOnlySet<Coord>? _blockedCells;
	private IReadOnlyDictionary<string, NonUnit>? _nonUnits;
	private BattleBoard? _board;

	public GridBasis StartFacing { get; private set; }

	public IReadOnlyList<IAction> Actions => _actions.Actions;

	public IReadOnlyList<IBattleAction> BattleActions =>
		_actions.Actions.Cast<IBattleAction>().ToList();

	public BattleBoard Board =>
		_board ?? throw new InvalidOperationException("Call BeginTurn before planning.");

	public bool HasBoard => _board is not null;

	public BattlePlanContext Context => new(BattleActions, StartFacing, _tags);

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

		var actor = roster.First(unit => unit.State.Id == ownerId);
		StartFacing = GridBasis.From(
			actor.State.ForwardDirection,
			actor.State.UpDirection,
			actor.State.RightDirection);
		_actions.Clear();
		_tags.Clear();
		_board = BattleBoard.FromSnapshot(roster, nonUnits, grid, blockedCells);
	}

	public void CopyFrom(State actor, IEnumerable<IAction> actions)
	{
		StartFacing = GridBasis.From(
			actor.ForwardDirection,
			actor.UpDirection,
			actor.RightDirection);
		_actions.Clear();
		_tags.Clear();
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
		var battleAction = (IBattleAction)action;
		if (!battleAction.IsLegal(Board, Context))
			return false;

		_actions.Enqueue(action);
		ReplayState();
		return true;
	}

	public void ForceApplyAndEnqueue(IAction action)
	{
		EnsureBoard();
		_actions.Enqueue(action);
		ReplayState();
	}

	public bool TryUndoLast()
	{
		if (!_actions.TryPopLast(out _))
			return false;

		ReplayState();
		return true;
	}

	private void ReplayState()
	{
		EnsureTurnContext();
		_board = BattleBoard.FromSnapshot(_roster!, _nonUnits!, _grid!, _blockedCells!);
		PlanPipeline.TryApplyAll(BattleActions, _board, Context, _ownerId!);
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
