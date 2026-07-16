using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Planning;

/// <summary>
/// Plan-mode preview: simulates the current plan and queries legal actions.
/// </summary>
public static class Preview
{
	public static IReadOnlyList<Option> GetLegalMoves(PlayerController planning) =>
		GetLegalMoves(
			planning.Actor,
			planning.Opponent,
			planning.Grid,
			planning.Plan,
			planning.BlockedCells,
			planning.Actions);

	public static IReadOnlyList<Option> GetSelectionMoves(PlayerController planning) =>
		GetLegalMoves(
			planning.Actor,
			planning.Opponent,
			planning.Grid,
			planning.Plan,
			planning.BlockedCells,
			PlanActions.WithoutQueuedMove(planning.Actions));

	public static SimulatedTurn Simulate(PlayerController planning) =>
		BattlePlanExecutor.Simulate(
			planning.Actor,
			planning.Opponent,
			planning.Grid,
			planning.Plan,
			planning.BlockedCells);

	private static IReadOnlyList<Option> GetLegalMoves(
		Unit actor,
		Unit opponent,
		BoundedGrid grid,
		PlayerPlan plan,
		IReadOnlySet<Coord> blockedCells,
		IReadOnlyList<IBattleAction> actions)
	{
		var board = PlanSimulator.Simulate(
			actor,
			opponent,
			grid,
			actions,
			plan.StartFacing,
			blockedCells);

		return LegalActions.GetMoveOptions(board, plan.Context);
	}
}
