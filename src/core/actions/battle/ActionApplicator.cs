using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Slices;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Core.Actions.Battle;

public static class ActionApplicator
{
	public static bool TryApplyAll(
		IReadOnlyList<IAction> actions,
		BattleBoard board,
		BattlePlanContext context,
		Timeline timeline,
		string actorId)
	{
		context.TurnState.Clear();
		var applied = new List<IAction>();

		foreach (var action in actions)
		{
			var stepContext = new BattlePlanContext(applied, context.TurnState);
			if (!TryApplyOne(action, board, stepContext, timeline, actorId))
				return false;

			applied.Add(action);
		}

		return true;
	}

	public static bool TryApplyOne(
		IAction action,
		BattleBoard board,
		BattlePlanContext context,
		Timeline timeline,
		string actorId,
		bool checkLegal = true)
	{
		if (checkLegal && !action.IsLegal(board, context))
			return false;

		var slices = SystemAction.Is(actorId)
			? BattleSlices.ForSystem(board, timeline)
			: BattleSlices.For(board, actorId, context.TurnState, timeline);
		foreach (var effect in action.Resolve(board, context))
			effect.Apply(slices);

		return true;
	}

	public static void ApplyToLive(
		IReadOnlyList<IAction> actions,
		IReadOnlyList<Unit> roster,
		BoundedGrid grid,
		IDictionary<string, NonUnit> nonUnits,
		IReadOnlySet<Coord> blockedCells,
		Timeline timeline,
		string? actorId = null)
	{
		var board = BattleBoard.FromLive(roster, nonUnits, grid, blockedCells);
		var turnState = new TurnState();
		var applied = new List<IAction>();
		var context = new BattlePlanContext(applied, turnState);
		var phaseActions = WithPhaseEnd(actions, actorId);
		TryApplyAll(phaseActions, board, context, timeline, actorId!);
	}

	public static void ApplyCommittedAction(
		IAction action,
		IReadOnlyList<Unit> roster,
		BoundedGrid grid,
		IDictionary<string, NonUnit> nonUnits,
		IReadOnlySet<Coord> blockedCells,
		BattlePlanContext context,
		Timeline timeline,
		string actorId)
	{
		var board = BattleBoard.FromLive(roster, nonUnits, grid, blockedCells);
		TryApplyOne(action, board, context, timeline, actorId, checkLegal: false);
	}

	public static IReadOnlyList<IAction> WithPhaseEnd(IReadOnlyList<IAction> actions, string? actorId)
	{
		if (actorId is null)
			return actions;

		return new List<IAction>(actions) { new EndOfPhaseAction(actorId) };
	}
}
