using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement.Enums;

namespace GrimSpace.Battle.Presentation.Domains.Orientation;

public static class OrientationUi
{
	public static bool TryApplyRoll(BattleOrchestrator battle, ERollDirection direction)
	{
		var actor = battle.GetActiveActor();
		if (actor is null || !battle.CanAct(actor))
			return false;

		return battle.Sim.TryEnqueue(new RollAction(battle.PlayerId, direction));
	}

	public static bool TryApplyHeadingTurn(BattleOrchestrator battle, EHeadingTurn turn)
	{
		var actor = battle.GetActiveActor();
		if (actor is null || !battle.CanAct(actor))
			return false;

		return battle.Sim.TryEnqueue(new HeadingTurnAction(battle.PlayerId, turn));
	}
}
