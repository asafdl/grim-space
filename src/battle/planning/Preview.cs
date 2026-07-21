using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Planning;

/// <summary>
/// Plan-mode preview: reads the current planning board and queries legal actions.
/// </summary>
public static class Preview
{
	public static IReadOnlyList<Option> GetLegalMoves(PlayerController planning) =>
		GetLegalMoves(planning.Plan, planning.OwnerId);

	public static SimulatedTurn Simulate(PlayerController planning) =>
		planning.Plan.GetPreview(planning.OwnerId);

	public static IReadOnlyList<Option> GetLegalMoves(BattleSession plan, string actorId)
	{
		if (plan.Context.TurnState.IsMovePathStarted)
			return [];

		return LegalActions.GetMoveOptions(plan.Board, plan.Context, actorId);
	}
}
