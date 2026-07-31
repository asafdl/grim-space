using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;

namespace GrimSpace.Tests;

internal static class BattleTestActions
{
	public static bool TryEnqueueMovePath(BattleOrchestrator battle, Option option) =>
		TryEnqueueMovePath(battle.Sim, battle.PlayerId, option);

	public static bool TryEnqueueMovePath(BattleSimulation sim, string actorId, Option option)
	{
		var actor = sim.StateOf<ActorState>(actorId);
		try
		{
			var steps = MoveDef.StepsFromPath(
				actorId,
				BodyFrame.From(actor),
				actor.Position,
				option.Path);
			return sim.TryEnqueue(actions: [..steps]);
		}
		catch (InvalidOperationException)
		{
			return false;
		}
	}

	public static bool TryCommitPreview(BattleOrchestrator battle, out IReadOnlyList<IAction> actions)
	{
		if (!battle.Sim.TryCommit(out actions, out _))
			return false;

		actions = HeadingDef.Instance.Streamline(actions, battle.Sim.UndoGroups).ToList();
		return true;
	}
}
