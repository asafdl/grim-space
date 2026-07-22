using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
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
		var runtime = new ActorSession();
		foreach (var action in WithPhaseEnd(actions, actorId))
		{
			if (action is not IAction<BattleBoard, ActorSession> typed)
				continue;

			var board = BattleBoard.FromLive(roster, nonUnits, grid, blocked, timeline);
			typed.Apply(board, runtime);
		}
	}

	public static bool TryApplyOne(
		IAction action,
		BattleBoard board,
		ActorSession runtime,
		string actorId)
	{
		if (action is not IAction<BattleBoard, ActorSession> typed)
			return false;

		if (!typed.IsLegal(board, runtime))
			return false;

		typed.Apply(board, runtime);
		return true;
	}

	public static bool TryApplyAll(
		IReadOnlyList<IAction> actions,
		BattleBoard board,
		ActorSession runtime,
		string actorId)
	{
		foreach (var action in actions)
		{
			if (!TryApplyOne(action, board, runtime, actorId))
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
		ActorSession runtime,
		Timeline timeline,
		string actorId)
	{
		var board = BattleBoard.FromLive(roster, nonUnits, grid, blocked, timeline);
		if (action is IAction<BattleBoard, ActorSession> typed)
			typed.Apply(board, runtime);
	}

	private static IEnumerable<IAction> WithPhaseEnd(IReadOnlyList<IAction> actions, string actorId)
	{
		foreach (var action in actions)
			yield return action;

		if (!actions.OfType<EndOfPhaseAction>().Any())
			yield return new EndOfPhaseAction(actorId);
	}
}
