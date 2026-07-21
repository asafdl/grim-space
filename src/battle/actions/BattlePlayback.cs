using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public static class BattlePlayback
{
	public static IReadOnlyList<IBattleAction> WithPhaseEnd(
		IReadOnlyList<IBattleAction> actions,
		string? actorId)
	{
		if (actorId is null)
			return actions;

		return [..actions, new EndOfPhaseAction(actorId)];
	}
}
