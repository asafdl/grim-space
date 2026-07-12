using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Contexts;
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
	public static BattleBoard BuildPlanBoard(
		Unit player,
		Unit enemy,
		BoundedGrid grid,
		IReadOnlyList<IBattleAction> actions)
	{
		var board = BattleBoard.ForSimulation(player, enemy, grid);
		ApplyAll(actions, board);
		return board;
	}

	public static SimulatedTurn Simulate(
		Unit player,
		Unit enemy,
		BoundedGrid grid,
		IReadOnlyList<IBattleAction> actions)
	{
		var board = BuildPlanBoard(player, enemy, grid, actions);

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
		ICollection<Hazard> activeHazards)
	{
		var board = BattleBoard.ForCommit(player, enemy, grid, activeHazards);
		ApplyAll(actions, board);
	}

	private static void ApplyAll(IReadOnlyList<IBattleAction> actions, BattleBoard board) =>
		PlanExecutor.Apply<IBattleAction, BattleBoard, BattleSlices, BattlePlanContext>(
			actions,
			board,
			BattleSlices.From);
}
