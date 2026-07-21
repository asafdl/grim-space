using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Slices;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Core.Actions.Battle;

public static class ActionApplicator
{
	public static bool TryApplyAll(
		IReadOnlyList<IBattleAction> actions,
		BattleBoard board,
		TurnState state,
		Timeline timeline,
		string actorId)
	{
		state.Clear();
		var applied = new List<IAction>();

		foreach (var action in actions)
		{
			if (!TryApplyOne(action, board, state, applied, timeline, actorId))
				return false;

			applied.Add(action);
		}

		return true;
	}

	public static bool TryApplyOne(
		IBattleAction action,
		BattleBoard board,
		TurnState state,
		IEnumerable<IAction> applied,
		Timeline timeline,
		string actorId,
		bool checkLegal = true)
	{
		if (checkLegal && !action.IsLegal(board, state, applied))
			return false;

		ApplyOne(action, board, state, applied, timeline, actorId);
		return true;
	}

	public static void ApplyOne(
		IBattleAction action,
		BattleBoard board,
		TurnState state,
		IEnumerable<IAction> applied,
		Timeline timeline,
		string actorId,
		bool checkLegal = false)
	{
		if (checkLegal && !action.IsLegal(board, state, applied))
			return;

		var slices = BattleSliceFactory.Create(board, state, timeline, actorId);
		foreach (var effect in action.Resolve(board, state, applied))
			effect.Apply(slices);
	}

	public static void ApplyToLive(
		IReadOnlyList<IBattleAction> actions,
		IReadOnlyList<Unit> roster,
		BoundedGrid grid,
		IDictionary<string, NonUnit> nonUnits,
		IReadOnlySet<Coord> blockedCells,
		Timeline timeline,
		string? actorId = null)
	{
		var board = BattleBoard.FromLive(roster, nonUnits, grid, blockedCells);
		var state = new TurnState();
		var phaseActions = WithPhaseEnd(actions, actorId);
		TryApplyAll(phaseActions, board, state, timeline, actorId!);
	}

	public static void ApplyCommittedAction(
		IBattleAction action,
		IReadOnlyList<Unit> roster,
		BoundedGrid grid,
		IDictionary<string, NonUnit> nonUnits,
		IReadOnlySet<Coord> blockedCells,
		TurnState state,
		IEnumerable<IAction> applied,
		Timeline timeline,
		string actorId)
	{
		var board = BattleBoard.FromLive(roster, nonUnits, grid, blockedCells);
		ApplyOne(action, board, state, applied, timeline, actorId, checkLegal: false);
	}

	public static IReadOnlyList<IBattleAction> WithPhaseEnd(
		IReadOnlyList<IBattleAction> actions,
		string? actorId)
	{
		if (actorId is null)
			return actions;

		var expanded = new List<IBattleAction>(actions) { new EndOfPhaseAction(actorId) };
		return expanded;
	}
}
