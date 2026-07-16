using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Core.Actions.Battle;

public sealed class SimulatedTurn
{
	public required State Player { get; init; }
	public required State Enemy { get; init; }
	public required IReadOnlyList<Hazard> Hazards { get; init; }
}

public static class BattlePlanExecutor
{
	public static SimulatedTurn Simulate(
		Unit player,
		Unit enemy,
		BoundedGrid grid,
		PlayerPlan plan,
		IReadOnlySet<Coord> blockedCells)
	{
		var board = PlanSimulator.Simulate(
			player,
			enemy,
			grid,
			plan.Actions,
			plan.StartFacing,
			blockedCells);

		return new SimulatedTurn
		{
			Player = board.Player,
			Enemy = board.Enemy,
			Hazards = board.Hazards.ToList(),
		};
	}

	public static void Apply(
		IReadOnlyList<IBattleAction> actions,
		Unit player,
		Unit enemy,
		BoundedGrid grid,
		ICollection<Hazard> activeHazards,
		IReadOnlySet<Coord> blockedCells,
		PlayerPlan plan) =>
		Apply(actions, player, enemy, grid, activeHazards, blockedCells, plan.StartFacing);

	public static void Apply(
		IReadOnlyList<IBattleAction> actions,
		Unit actor,
		Unit opponent,
		BoundedGrid grid,
		ICollection<Hazard> activeHazards,
		IReadOnlySet<Coord> blockedCells,
		GridBasis? yawSettleFacing = null)
	{
		var board = BattleBoard.ForCommit(actor, opponent, grid, activeHazards, blockedCells);
		PlanSimulator.Apply(actions, board);

		if (actions.Count == 0)
			board.Player.MomentumLevel = System.Math.Max(board.Player.MomentumLevel - 1, 0);

		if (yawSettleFacing is { } facing)
			Orientation.SettleNetYaw(board.Player, facing);
	}
}
