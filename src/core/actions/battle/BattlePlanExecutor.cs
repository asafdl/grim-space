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
		PlayerPlan plan)
	{
		var board = PlanSimulator.Simulate(player, enemy, grid, plan.Actions, plan.StartFacing);

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
		PlayerPlan plan) =>
		Apply(actions, player, enemy, grid, activeHazards, plan.StartFacing);

	public static void Apply(
		IReadOnlyList<IBattleAction> actions,
		Unit actor,
		Unit opponent,
		BoundedGrid grid,
		ICollection<Hazard> activeHazards,
		GridBasis? yawSettleFacing = null)
	{
		var board = BattleBoard.ForCommit(actor, opponent, grid, activeHazards);
		PlanSimulator.Apply(actions, board);

		if (yawSettleFacing is { } facing)
			Orientation.SettleNetYaw(board.Player, facing);
	}
}
