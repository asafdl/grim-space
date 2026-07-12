using GrimSpace.Battle.Actions.Contexts;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Actions;

public sealed class SimulatedTurn
{
	public required State Player { get; init; }
	public required State Enemy { get; init; }
	public required IReadOnlyList<Hazard> Hazards { get; init; }
}

public static class PlanExecutor
{
	public static SimulatedTurn Simulate(
		Unit player,
		Unit enemy,
		BoundedGrid grid,
		IReadOnlyList<IAction> actions)
	{
		var board = ActionBoard.ForSimulation(player, enemy, grid);
		ApplyAll(actions, board);

		return new SimulatedTurn
		{
			Player = board.Player,
			Enemy = board.Enemy,
			Hazards = board.Hazards.ToList(),
		};
	}

	public static void Apply(
		IReadOnlyList<IAction> actions,
		Unit player,
		Unit enemy,
		BoundedGrid grid,
		ICollection<Hazard> activeHazards)
	{
		var board = ActionBoard.ForCommit(player, enemy, grid, activeHazards);
		ApplyAll(actions, board);
	}

	private static void ApplyAll(IReadOnlyList<IAction> actions, ActionBoard board)
	{
		var slices = ActionSlices.From(board);

		foreach (var action in actions)
		{
			foreach (var effect in action.Resolve(board))
				effect.Apply(slices);
		}
	}
}
