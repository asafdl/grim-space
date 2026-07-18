using GrimSpace.Battle.Board;
using GrimSpace.Battle.Ids;
using GrimSpace.Battle.Movement;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Units;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Player;

/// <summary>
/// Player turn planning: action queue and legality checks.
/// </summary>
public sealed class PlayerController
{
	private readonly TurnPlanner _plan = new();
	private readonly Unit _player;
	private readonly Unit _enemy;
	private readonly IReadOnlyList<Unit> _roster;
	private readonly IReadOnlyDictionary<string, NonUnit> _nonUnits;
	private readonly BoundedGrid _grid;
	private readonly IReadOnlySet<Coord> _blockedCells;
	private readonly Func<Unit, bool> _canAct;
	private readonly Func<Unit?> _getActivePlayer;

	public PlayerController(
		Unit player,
		Unit enemy,
		IReadOnlyList<Unit> roster,
		IReadOnlyDictionary<string, NonUnit> nonUnits,
		BoundedGrid grid,
		IReadOnlySet<Coord> blockedCells,
		Func<Unit, bool> canAct,
		Func<Unit?> getActivePlayer)
	{
		_player = player;
		_enemy = enemy;
		_roster = roster;
		_nonUnits = nonUnits;
		_grid = grid;
		_blockedCells = blockedCells;
		_canAct = canAct;
		_getActivePlayer = getActivePlayer;
	}

	public string OwnerId => _player.State.Id;
	public IReadOnlySet<Coord> BlockedCells => _blockedCells;
	public Unit Actor => _player;
	public Unit Opponent => _enemy;
	public BoundedGrid Grid => _grid;
	public TurnPlanner Plan => _plan;
	public BattleBoard Board => _plan.Board;
	public IReadOnlyList<IAction> Actions => _plan.Actions;
	public BattlePlanContext Context => _plan.Context;
	public int MissilesRemainingThisTurn => Board.StateOf(OwnerId).MissilesRemaining;

	public void BeginTurn(int turnStartTick) =>
		_plan.BeginTurn(OwnerId, _roster, _grid, _nonUnits, _blockedCells, turnStartTick);

	public FinalizedPlan FinalizePlan() => new(_plan.Actions.ToList());

	public bool CanAct(Unit unit) => _canAct(unit);

	public Unit? GetActiveActor() => _getActivePlayer();

	public bool IsLegal(IAction action)
	{
		var player = _getActivePlayer();
		if (player is null || !_canAct(player))
			return false;

		return BattleActionFactory.WithOwner(OwnerId, action).IsLegal(Board, _plan.Context);
	}

	public bool TryEnqueue(IAction action)
	{
		var player = _getActivePlayer();
		if (player is null || !_canAct(player))
			return false;

		if (action is FlakAction && _plan.Actions.Any(queued => queued is FlakAction))
			return false;

		if (action is RailgunAction && _plan.Actions.Any(queued => queued is RailgunAction))
			return false;

		return _plan.TryApplyAndEnqueue(BattleActionFactory.WithOwner(OwnerId, action));
	}

	public bool TryEnqueueMovePath(Option option) => _plan.TryEnqueueMovePath(OwnerId, option);

	public bool TryUndoLast() => _plan.TryUndoLast();
}

public readonly record struct FinalizedPlan(IReadOnlyList<IAction> Actions);
