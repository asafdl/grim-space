using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Player;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Planning;

/// <summary>
/// Plan-mode preview: reads the current planning board and queries legal actions.
/// </summary>
public static class Preview
{
	public static IReadOnlyList<Option> GetLegalMoves(PlayerController planning) =>
		GetLegalMoves(planning.Simulation, planning.OwnerId);

	public static SimulatedTurn Simulate(PlayerController planning) =>
		new() { Board = planning.Board, ActorId = planning.OwnerId };

	public static IReadOnlyList<Option> GetLegalMoves(PlanSimulation plan, string actorId)
	{
		var ctx = BattleActionContext.For(plan.PreviewWorld, plan.PreviewRuntime, actorId);
		if (ctx.TurnState.IsMovePathStarted)
			return [];

		return LegalActions.GetMoveOptions(plan.PreviewWorld, ctx, actorId);
	}
}
