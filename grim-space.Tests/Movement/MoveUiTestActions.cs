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
		var options = ui.MoveUi.GetMoveOptions(battle.Sim.Actions).ToList();
		var option = options.First(candidate => candidate.EndPosition == endPosition);
		var actorState = battle.Sim.StateOf<ActorState>(battle.PlayerId);
		var steps = MoveUi.ToMoveActions(battle.PlayerId, actorState, option);
		if (steps is null || !battle.Sim.TryEnqueue(actions: [..steps]))
			return false;

		state.CommittedMovePath = option.Path;
		state.ClearHovers();
		return true;
	}

	public static bool ClickMove(BattleUi ui, Coord endPosition) =>
		ClickMove(ui, ui.State, endPosition);

	public static bool ClickHeading(BattleOrchestrator battle, EHeadingTurn turn) =>
		battle.Sim.TryEnqueue(new HeadingTurnAction(battle.PlayerId, turn));
}
