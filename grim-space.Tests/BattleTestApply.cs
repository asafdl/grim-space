using GrimSpace.Battle.Board;
using GrimSpace.Battle.Turn;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Actions;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Tests;

internal static class BattleTestApply
{
	public static void ApplyToLive(
		IReadOnlyList<IAction> actions,
		IReadOnlyList<Unit> roster,
		BoundedGrid grid,
		IDictionary<string, NonUnit> nonUnits,
		IReadOnlySet<Coord> blocked,
		Timeline timeline,
		string actorId)
	{
		var phaseContext = new TurnPhaseContext();
		foreach (var action in BattlePlayback.WithPhaseEnd(actions, actorId))
		{
			if (!BattleActionRunner.IsKnown(action))
				continue;

			var board = BattleBoard.FromLive(roster, nonUnits, grid, blocked, timeline);
			var ctx = BattleActionContext.For(board, phaseContext, action.OwnerId);
			BattleActionRunner.Apply(action, ctx);
		}
	}

	public static bool TryApplyOne(
		IAction action,
		BattleBoard board,
		TurnPhaseContext phaseContext,
		string actorId)
	{
		var ctx = BattleActionContext.For(board, phaseContext, actorId);
		return BattleActionRunner.TryApply(action, ctx);
	}

	public static bool TryApplyAll(
		IReadOnlyList<IAction> actions,
		BattleBoard board,
		TurnPhaseContext phaseContext,
		string actorId)
	{
		foreach (var action in actions)
		{
			if (!TryApplyOne(action, board, phaseContext, actorId))
				return false;
		}

		return true;
	}

	public static void ApplyCommittedAction(
		IAction action,
		IReadOnlyList<Unit> roster,
		BoundedGrid grid,
		IDictionary<string, NonUnit> nonUnits,
		IReadOnlySet<Coord> blocked,
		TurnPhaseContext phaseContext,
		Timeline timeline,
		string actorId)
	{
		var board = BattleBoard.FromLive(roster, nonUnits, grid, blocked, timeline);
		var ctx = BattleActionContext.For(board, phaseContext, actorId);
		BattleActionRunner.Apply(action, ctx);
	}
}
