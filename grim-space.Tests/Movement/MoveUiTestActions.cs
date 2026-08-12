using GrimSpace.Battle;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Presentation;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Movement;

internal static class MoveUiTestActions
{
	public static bool ClickMove(BattleOrchestrator battle, Coord endPosition) =>
		BattleTestCommands.Move(battle, endPosition);

	public static bool ClickHeading(BattleOrchestrator battle, EHeadingTurn turn) =>
		BattleTestCommands.Turn(battle, turn);
}
