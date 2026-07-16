using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Contexts;
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
		GridBasis? yawSettleFacing = null,
		string? actorId = null)
	{
		var board = BattleBoard.FromLive(roster, nonUnits, grid, blockedCells);
		PlanSimulator.Apply(actions, board);

		if (actions.Count == 0 && actorId is not null)
			board.StateOf(actorId).MomentumLevel = System.Math.Max(board.StateOf(actorId).MomentumLevel - 1, 0);

		if (yawSettleFacing is { } facing && actorId is not null)
			Orientation.SettleNetYaw(board.StateOf(actorId), facing);
	}

	public static void Apply(
		IBattleAction action,
		IReadOnlyList<Unit> roster,
		BoundedGrid grid,
		IDictionary<string, NonUnit> nonUnits,
		IReadOnlySet<Coord> blockedCells)
	{
		var board = BattleBoard.FromLive(roster, nonUnits, grid, blockedCells);
		PlanExecutor.Apply(action, board);
	}
}
