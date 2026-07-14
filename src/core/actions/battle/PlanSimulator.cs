using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Contexts;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Core.Actions.Battle;

/// <summary>
/// Applies queued actions to cloned battle state (rules simulation, not presentation).
/// </summary>
public static class PlanSimulator
{
	public static void Apply(IReadOnlyList<IBattleAction> actions, BattleBoard board) =>
		PlanExecutor.Apply<IBattleAction, BattleBoard, BattleSlices, BattlePlanContext>(
			actions,
			board,
			BattleSlices.From);

	public static BattleBoard BuildBoard(
		Unit actor,
		Unit opponent,
		BoundedGrid grid,
		IReadOnlyList<IBattleAction> actions,
		bool excludeMoves = false)
	{
		var board = BattleBoard.ForSimulation(actor, opponent, grid);
		var toApply = excludeMoves
			? actions.Where(action => action is not MoveAction).ToList()
			: actions;
		Apply(toApply, board);
		return board;
	}

	public static BattleBoard Simulate(
		Unit actor,
		Unit opponent,
		BoundedGrid grid,
		IReadOnlyList<IBattleAction> actions,
		GridBasis startFacing,
		bool excludeMoves = false)
	{
		var board = BuildBoard(actor, opponent, grid, actions, excludeMoves);
		Orientation.SettleNetYaw(board.Player, startFacing);
		return board;
	}
}
