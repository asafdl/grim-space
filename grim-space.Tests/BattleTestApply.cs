using GrimSpace.Battle.Board;
using GrimSpace.Battle.Turn;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Slices;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
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
			if (action is not IBattleAction battleAction)
				continue;

			var board = BattleBoard.FromLive(roster, nonUnits, grid, blocked, timeline);
			var ctx = BattleActionContext.For(board, phaseContext, action.OwnerId);
			SimulationRunner<BattleActionContext, BattleSlices, IBattleAction>.Step(ctx, battleAction);
		}
	}

	public static bool TryApplyOne(
		IBattleAction action,
		BattleBoard board,
		TurnPhaseContext phaseContext,
		string actorId)
	{
		var ctx = BattleActionContext.For(board, phaseContext, actorId);
		return SimulationRunner<BattleActionContext, BattleSlices, IBattleAction>.TryStep(ctx, action);
	}

	public static bool TryApplyAll(
		IReadOnlyList<IBattleAction> actions,
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
		IBattleAction action,
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
		SimulationRunner<BattleActionContext, BattleSlices, IBattleAction>.Step(ctx, action);
	}
}
