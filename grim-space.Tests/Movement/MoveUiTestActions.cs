using GrimSpace.Battle;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Movement;

internal static class MoveUiTestActions
{
	public static bool ClickMove(BattleUi ui, InteractionState state, Coord endPosition)
	{
		_ = state;
		return BattleTestCommands.Move(ui.Battle, endPosition);
	}

	public static bool ClickMove(BattleUi ui, Coord endPosition) =>
		ClickMove(ui, ui.State, endPosition);

	public static bool ClickHeading(BattleOrchestrator battle, EHeadingTurn turn) =>
		BattleTestCommands.Turn(battle, turn);
}
