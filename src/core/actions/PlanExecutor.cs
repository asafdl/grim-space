using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Core.Actions.Battle.Contexts;

namespace GrimSpace.Core.Actions;

public static class PlanExecutor
{
	public static void Apply(IReadOnlyList<IBattleAction> actions, BattleBoard board)
	{
		foreach (var action in actions)
		{
			var actorId = ((IAction)action).OwnerId;
			var slices = BattleSlices.For(board, actorId);

			foreach (var effect in action.Resolve(board))
				effect.Apply(slices);
		}
	}

	public static void Apply(IBattleAction action, BattleBoard board) =>
		Apply([action], board);
}
