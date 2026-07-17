using GrimSpace.Core.Actions.Battle.Contexts;

namespace GrimSpace.Core.Actions.Battle;

public static class PlanPipeline
{
	public static bool TryApplyAll(
		IReadOnlyList<IBattleAction> actions,
		BattleBoard board,
		BattlePlanContext context,
		string actorId)
	{
		context.Tags.Clear();

		foreach (var action in actions)
		{
			if (!TryApplyOne(action, board, context, actorId))
				return false;
		}

		return true;
	}

	public static bool TryApplyOne(
		IBattleAction action,
		BattleBoard board,
		BattlePlanContext context,
		string actorId,
		bool checkLegal = true)
	{
		if (checkLegal && !action.IsLegal(board, context))
			return false;

		var slices = BattleSlices.For(board, actorId);
		foreach (var effect in action.Resolve(board, context))
			effect.Apply(slices);

		return true;
	}

	public static void RunPhaseEnd(BattleBoard board, IReadOnlyList<IBattleAction> plan, string actorId)
	{
		if (!plan.Any(action => action is MoveAction))
			board.StateOf(actorId).MomentumLevel = System.Math.Max(board.StateOf(actorId).MomentumLevel - 1, 0);
	}
}
