using GrimSpace.Battle.Actions;
using GrimSpace.Battle.World;

namespace GrimSpace.Battle.Presentation.Domains.Turn;

public static class TurnUi
{
	public static bool TryUndo(BattleOrchestrator battle, Interaction.InteractionState state)
	{
		if (battle.Sim.Actions.Count == 0)
			return false;

		var undone = battle.Sim.Actions[^1];
		if (!battle.Sim.TryUndoLast())
			return false;

		state.ClearHovers();
		if (undone is MoveStepAction)
			state.CommittedMovePath = [];

		return true;
	}

	public static BattleWorld GetPreviewWorld(BattleOrchestrator battle)
	{
		var peek = battle.Sim.Peek(EndOfPhaseDef.Instance.Bind(battle.PlayerId));
		return peek?.World ?? battle.Sim.World;
	}
}
