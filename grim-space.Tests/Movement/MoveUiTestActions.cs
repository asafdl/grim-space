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
	public static bool ClickMove(BattleUi ui, InteractionState state, Coord endPosition)
	{
		var battle = ui.Battle;
		var options = ui.MoveUi.GetMovePaths(battle.Sim, battle.PlayerId, battle.Sim.Actions).ToList();
		var option = options.First(candidate => candidate.EndPosition == endPosition);
		if (!battle.Sim.TryEnqueue(actions: [..option.Steps]))
			return false;

		state.CommittedMovePath = option.Cells;
		state.ClearHovers();
		return true;
	}

	public static bool ClickMove(BattleUi ui, Coord endPosition) =>
		ClickMove(ui, ui.State, endPosition);

	public static bool ClickHeading(BattleOrchestrator battle, EHeadingTurn turn) =>
		battle.Sim.TryEnqueue(new HeadingTurnAction(battle.PlayerId, turn));
}
