using GrimSpace.Battle.Board;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Core.Actions.Battle;

public sealed class SimulatedTurn
{
	public required BattleBoard Board { get; init; }
	public required string ActorId { get; init; }

	public State Actor => Board.StateOf(ActorId);

	public IEnumerable<Hazard> Hazards => Board.TurnHazards;
}

public static class BattlePlanExecutor
{
	public static SimulatedTurn Simulate(UnitPlan plan, string actorId) =>
		new()
		{
			Board = plan.Board,
			ActorId = actorId,
		};

	public static void Apply(
		IReadOnlyList<IBattleAction> actions,
		IReadOnlyList<Unit> roster,
		BoundedGrid grid,
		IDictionary<string, NonUnit> nonUnits,
		IReadOnlySet<Coord> blockedCells,
		UnitPlan plan) =>
		Apply(actions, roster, grid, nonUnits, blockedCells, plan.StartFacing, plan.OwnerId);

	public static void Apply(
		IReadOnlyList<IBattleAction> actions,
		IReadOnlyList<Unit> roster,
		BoundedGrid grid,
		IDictionary<string, NonUnit> nonUnits,
		IReadOnlySet<Coord> blockedCells,
		GridBasis startFacing,
		string? actorId = null)
	{
		var board = BattleBoard.FromLive(roster, nonUnits, grid, blockedCells);
		var tags = new BattleTurnTags();
		var context = new BattlePlanContext(actions, startFacing, tags);
		PlanPipeline.TryApplyAll(actions, board, context, actorId!);

		if (actorId is not null)
			PlanPipeline.RunPhaseEnd(board, actions, actorId);
	}

	public static void ApplyCommittedAction(
		IBattleAction action,
		IReadOnlyList<Unit> roster,
		BoundedGrid grid,
		IDictionary<string, NonUnit> nonUnits,
		IReadOnlySet<Coord> blockedCells,
		BattlePlanContext context,
		string actorId)
	{
		var board = BattleBoard.FromLive(roster, nonUnits, grid, blockedCells);
		PlanPipeline.TryApplyOne(action, board, context, actorId, checkLegal: false);
	}
}
