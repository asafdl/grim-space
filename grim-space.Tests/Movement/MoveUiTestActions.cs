using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Movement;

internal static class MoveUiTestActions
{
	public static bool ClickMove(BattleOrchestrator battle, InteractionState state, Coord endPosition)
	{
		var options = battle.MoveUi.GetMoveOptions(battle.Sim.Actions).ToList();
		var option = options.First(candidate => candidate.EndPosition == endPosition);
		return MoveUi.TryApply(battle, state, option);
	}

	public static bool ClickMove(BattleUi ui, Coord endPosition)
	{
		var options = MoveUi.GetMoveOptions(ui.Battle, ui.Battle.GetActiveActor()).ToList();
		var index = options.FindIndex(option => option.EndPosition == endPosition);
		return ui.TryQueueMove(index, options);
	}

	public static bool ClickHeading(BattleOrchestrator battle, EHeadingTurn turn) =>
		battle.Sim.TryEnqueue(new HeadingTurnAction(battle.PlayerId, turn));
}
