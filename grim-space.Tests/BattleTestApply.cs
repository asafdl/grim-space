using GrimSpace.Battle.Board;
using GrimSpace.Battle.Slices;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Tests;

internal static class BattleTestApply
{
	public static void ApplyToLive(
		IReadOnlyList<IBattleAction> actions,
		IReadOnlyList<Unit> roster,
		BoundedGrid grid,
		IDictionary<string, NonUnit> nonUnits,
		IReadOnlySet<Coord> blocked,
		Timeline timeline,
		string actorId)
	{
		var turnState = new TurnState();
		foreach (var action in BattlePlayback.WithPhaseEnd(actions, actorId))
		{
			var board = BattleBoard.FromLive(roster, nonUnits, grid, blocked, timeline);
			var ctx = BattleActionContext.For(board, turnState, action.OwnerId);
			SimulationRunner<BattleActionContext, BattleSlices, IBattleAction>.Step(ctx, action);
		}
	}

	public static bool TryApplyOne(
		IBattleAction action,
		BattleBoard board,
		TurnState turnState,
		string actorId)
	{
		var ctx = BattleActionContext.For(board, turnState, actorId);
		return SimulationRunner<BattleActionContext, BattleSlices, IBattleAction>.TryStep(ctx, action);
	}

	public static bool TryApplyAll(
		IReadOnlyList<IBattleAction> actions,
		BattleBoard board,
		TurnState turnState,
		string actorId)
	{
		foreach (var action in actions)
		{
			if (!TryApplyOne(action, board, turnState, actorId))
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
		TurnState turnState,
		Timeline timeline,
		string actorId)
	{
		var board = BattleBoard.FromLive(roster, nonUnits, grid, blocked, timeline);
		var ctx = BattleActionContext.For(board, turnState, actorId);
		SimulationRunner<BattleActionContext, BattleSlices, IBattleAction>.Step(ctx, action);
	}
}
