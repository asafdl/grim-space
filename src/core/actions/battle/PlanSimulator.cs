using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Contexts;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Core.Actions.Battle;

/// <summary>
/// Replays queued actions on a turn-start board snapshot (planning / legality).
/// </summary>
public static class PlanSimulator
{
	public static void Apply(IReadOnlyList<IBattleAction> actions, BattleBoard board) =>
		PlanExecutor.Apply(actions, board);

	public static BattleBoard BuildBoard(
		IReadOnlyList<Unit> roster,
		BoundedGrid grid,
		IReadOnlyList<IBattleAction> actions,
		IReadOnlySet<Coord> blockedCells,
		string actorId,
		IReadOnlyDictionary<string, NonUnit>? nonUnits = null,
		bool excludeMoves = false)
	{
		var board = BattleBoard.FromSnapshot(roster, nonUnits ?? new Dictionary<string, NonUnit>(), grid, blockedCells);
		var toApply = excludeMoves
			? actions.Where(action => action is not MoveAction).ToList()
			: actions;
		Apply(toApply, board);
		return board;
	}

	public static BattleBoard Simulate(
		IReadOnlyList<Unit> roster,
		BoundedGrid grid,
		IReadOnlyList<IBattleAction> actions,
		GridBasis startFacing,
		IReadOnlySet<Coord> blockedCells,
		string actorId,
		IReadOnlyDictionary<string, NonUnit>? nonUnits = null,
		bool excludeMoves = false)
	{
		var board = BuildBoard(roster, grid, actions, blockedCells, actorId, nonUnits, excludeMoves);
		Orientation.SettleNetYaw(board.StateOf(actorId), startFacing);
		return board;
	}
}
