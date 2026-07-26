using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Core.Actions;

namespace GrimSpace.Tests;

internal static class BattleTestActions
{
	public static bool TryEnqueueMovePath(BattleOrchestrator battle, Option option)
	{
		var steps = MoveUi.Translate(battle.Sim, battle.PlayerId, option);
		return steps is not null && battle.Sim.TryEnqueue(actions: [..steps]);
	}

	public static bool TryEnqueueMovePath(BattleSimulation sim, string actorId, Option option)
	{
		var steps = MoveUi.Translate(sim, actorId, option);
		return steps is not null && sim.TryEnqueue(actions: [..steps]);
	}

	public static bool TryCommitPreview(BattleOrchestrator battle, out IReadOnlyList<IAction> actions)
	{
		if (!battle.Sim.TryCommit(out actions, out _))
			return false;

		actions = HeadingDef.Instance.Streamline(actions, battle.Sim.UndoGroups).ToList();
		return true;
	}
}
