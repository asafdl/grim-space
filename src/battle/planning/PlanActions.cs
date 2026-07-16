using GrimSpace.Core.Actions.Battle;

namespace GrimSpace.Battle.Planning;

internal static class PlanActions
{
	public static IReadOnlyList<IBattleAction> WithoutQueuedMove(IReadOnlyList<IBattleAction> actions)
	{
		for (var i = 0; i < actions.Count; i++)
		{
			if (actions[i] is MoveAction)
				return actions.Take(i).ToList();
		}

		return actions;
	}
}
