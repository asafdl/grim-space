using GrimSpace.Battle.Movement;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Units;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Player;

/// <summary>
/// Player turn planning: action queue, enqueue policy, and legality checks.
/// </summary>
public sealed class PlayerController
{
	private readonly PlayerPlan _plan = new();
	private readonly Unit _player;
	private readonly Unit _enemy;
	private readonly BoundedGrid _grid;
	private readonly Func<Unit, bool> _canAct;
	private readonly Func<Unit?> _getActivePlayer;

	public PlayerController(
		Unit player,
		Unit enemy,
		BoundedGrid grid,
		Func<Unit, bool> canAct,
		Func<Unit?> getActivePlayer)
	{
		_player = player;
		_enemy = enemy;
		_grid = grid;
		_canAct = canAct;
		_getActivePlayer = getActivePlayer;
	}

	public Unit Actor => _player;
	public Unit Opponent => _enemy;
	public BoundedGrid Grid => _grid;
	public PlayerPlan Plan => _plan;
	public IReadOnlyList<IBattleAction> Actions => _plan.Actions;
	public BattlePlanContext Context => _plan.Context;
	public GridBasis StartFacing => _plan.StartFacing;
	public int MissilesRemainingThisTurn => _plan.Context.MissilesRemaining;

	public void ResetFrom(State player) => _plan.ResetFrom(player);

	public FinalizedPlan FinalizePlan() =>
		new(_plan.Actions.ToList(), _plan.Context.StartFacing);

	public bool CanAct(Unit unit) => _canAct(unit);

	public Unit? GetActiveActor() => _getActivePlayer();

	public bool IsLegal(IBattleAction action)
	{
		var player = _getActivePlayer();
		if (player is null || !_canAct(player))
			return false;

		var board = action is MoveAction
			? LegalityBoard(excludeMoves: true)
			: CommittedPlanBoard();
		return action.IsLegal(board, _plan.Context);
	}

	public bool TryEnqueue(IBattleAction action)
	{
		var player = _getActivePlayer();
		if (player is null || !_canAct(player))
			return false;

		if (action is HeadingTurnAction heading && Orientation.IsYawTurn(heading.Turn))
		{
			_plan.Enqueue(action);
			if (!CanAffordPlan())
			{
				_plan.TryUndoLast();
				return false;
			}

			return true;
		}

		if (!IsLegal(action))
			return false;

		_plan.Enqueue(action);
		return true;
	}

	public bool TryUndoLast() => _plan.TryUndoLast();

	private BattleBoard CommittedPlanBoard() =>
		PlanSimulator.Simulate(_player, _enemy, _grid, _plan.Actions, _plan.StartFacing);

	private BattleBoard LegalityBoard(bool excludeMoves) =>
		PlanSimulator.Simulate(_player, _enemy, _grid, _plan.Actions, _plan.StartFacing, excludeMoves);

	private bool CanAffordPlan() =>
		CommittedPlanBoard().Player.ActionPoints >= 0;
}

public readonly record struct FinalizedPlan(
	IReadOnlyList<IBattleAction> Actions,
	GridBasis StartFacing);
