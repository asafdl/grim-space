using GrimSpace.Core.Actions.Battle;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Player;

/// <summary>
/// Owns the player planning queue and UI-facing legality/preview queries.
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

	public IReadOnlyList<IBattleAction> PlannedActions => _plan.Actions;
	public int MissilesRemainingThisTurn => _plan.Context.MissilesRemaining;

	public void ResetFrom(State player) => _plan.ResetFrom(player);

	public FinalizedPlan FinalizePlan() =>
		new(_plan.Actions.ToList(), _plan.Context.StartFacing);

	public SimulatedTurn GetSimulation() =>
		BattlePlanExecutor.Simulate(_player, _enemy, _grid, _plan);

	public HashSet<Coord> GetPlannedHazardCells()
	{
		var cells = new HashSet<Coord>();
		foreach (var hazard in GetSimulation().Hazards)
			cells.UnionWith(hazard.Cells);

		return cells;
	}

	public HashSet<Coord> GetMissilePreviewCells(Coord center)
	{
		var hazard = Hazard.MissileZone(
			center,
			_grid,
			CombatConfig.MissileRadius,
			CombatConfig.MissileDamage,
			CombatConfig.MissileMomentumLoss);
		return hazard.Cells;
	}

	public Preview? GetMovementPreview(Unit unit)
	{
		if (!_canAct(unit))
			return null;

		var board = GetPlanBoard();
		var context = _plan.Context;
		return new Preview
		{
			Options = LegalActions.GetMoveOptions(board, context),
		};
	}

	public (Unit? Unit, Preview? Preview) GetActivePlayerPreview()
	{
		var unit = _getActivePlayer();
		if (unit is null)
			return (null, null);

		return (unit, GetMovementPreview(unit));
	}

	public HashSet<Coord> GetRailgunTargetCells(Unit? activeUnit)
	{
		var cells = new HashSet<Coord>();
		if (activeUnit is null || !_canAct(activeUnit))
			return cells;

		var board = GetPlanBoard();
		var context = _plan.Context;
		if (!LegalActions.IsRailgunAvailable(board, context))
			return cells;

		cells.Add(board.Enemy.Position);
		return cells;
	}

	public bool IsLegal(IBattleAction action)
	{
		var player = _getActivePlayer();
		return player is not null && _canAct(player) && action.IsLegal(GetPlanBoard(), _plan.Context);
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

	public HashSet<Coord> GetValidMissileCells(EMissileMount mount)
	{
		var player = _getActivePlayer();
		if (player is null || !_canAct(player))
			return [];

		return LegalActions.GetMissileCells(GetPlanBoard(), _plan.Context, mount);
	}

	public bool TryUndoLast() => _plan.TryUndoLast();

	private BattleBoard GetPlanBoard() =>
		BattlePlanExecutor.BuildPlanBoard(_player, _enemy, _grid, _plan);

	private bool CanAffordPlan() =>
		GetPlanBoard().Player.ActionPoints >= 0;
}

public readonly record struct FinalizedPlan(
	IReadOnlyList<IBattleAction> Actions,
	GridBasis StartFacing);
