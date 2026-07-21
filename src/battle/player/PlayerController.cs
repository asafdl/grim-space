using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Ids;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Planning;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Units;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Player;

/// <summary>
/// Player turn planning: action queue and legality checks.
/// </summary>
public sealed class PlayerController
{
	private readonly PlanSimulation _plan;
	private string _ownerId = string.Empty;
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
		_plan = new PlanSimulation(ExpandPlayback);
	}

	public string OwnerId => _player.State.Id;
	public IReadOnlySet<Coord> BlockedCells => _blockedCells;
	public Unit Actor => _player;
	public Unit Opponent => _enemy;
	public BoundedGrid Grid => _grid;
	public PlanSimulation Simulation => _plan;
	public BattleBoard Board => _plan.PreviewWorld;
	public IReadOnlyList<IAction> Actions => _plan.Actions;
	public BattleActionContext Context => BattleActionContext.For(Board, _plan.PreviewRuntime, _ownerId);
	public Timeline PreviewTimeline => Board.Timeline;
	public int TurnStartTick => _plan.AnchorTick;
	public int MissilesRemainingThisTurn => Board.StateOf(OwnerId).MissilesRemaining;

	public void BeginTurn(int turnStartTick)
	{
		_ownerId = OwnerId;
		var anchor = BattleBoard.FromSnapshot(_roster, _nonUnits, _grid, _blockedCells);
		_plan.Begin(anchor, turnStartTick);
	}

	public FinalizedPlan FinalizePlan() => new(_plan.Actions.ToList());

	public bool CanAct(Unit unit) => _canAct(unit);

	public Unit? GetActiveActor() => _getActivePlayer();

	public bool IsLegal(IAction action)
	{
		var player = _getActivePlayer();
		if (player is null || !_canAct(player))
			return false;

		if (BattleActionFactory.WithOwner(OwnerId, action) is not IBattleAction battleAction)
			return false;

		var ctx = BattleActionContext.For(Board, Context.TurnState, battleAction.OwnerId);
		return battleAction.IsLegal(ctx);
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

		return BattleActionFactory.WithOwner(OwnerId, action) is IBattleAction battleAction
			&& _plan.TryEnqueue(battleAction);
	}

	public bool TryEnqueueMovePath(Option option) =>
		_plan.TryEnqueue(new MovePathAction(OwnerId, option));

	public bool TryUndoLast() => _plan.TryUndoLast();

	private IReadOnlyList<IBattleAction> ExpandPlayback(IReadOnlyList<IBattleAction> actions) =>
		BattlePlayback.WithPhaseEnd(actions, string.IsNullOrEmpty(_ownerId) ? null : _ownerId);
}

public readonly record struct FinalizedPlan(IReadOnlyList<IAction> Actions);
