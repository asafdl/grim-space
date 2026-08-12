using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;

namespace GrimSpace.Tests;

internal static class BattleTestActions
{
	public static bool TryEnqueueMovePath(BattleOrchestrator battle, MovePathSession path) =>
		battle.PlayerAgent.TryEnqueue(path.Steps.Cast<Core.Actions.IAction>().ToList());

	public static bool TryEnqueueMovePath(BattleSimulation sim, string actorId, MovePathSession path) =>
		sim.TryEnqueue(actions: [..path.Steps]);

	public static bool TryCommitPreview(BattleOrchestrator battle, out IReadOnlyList<IAction> actions)
	{
		var actor = battle.GetActiveUnit();
		if (actor is null)
		{
			actions = [];
			return false;
		}

		if (!battle.PlayerAgent.Commit())
		{
			actions = [];
			return false;
		}

		actions = battle.PlayerAgent.GetActions().GetAwaiter().GetResult();
		return true;
	}

	public static TurnReplay CommitAndResolve(BattleOrchestrator battle)
	{
		var actor = battle.GetActiveUnit()!;
		Assert.True(battle.PlayerAgent.Commit());
		return battle.ResolveTurn();
	}
}
