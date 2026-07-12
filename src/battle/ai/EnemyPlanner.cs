using GrimSpace.Math.Grid;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Units;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Ai;

public static class EnemyPlanner
{
	public static Option? ChooseMove(
		Unit unit,
		BoundedGrid grid,
		IReadOnlySet<Coord> hazardCells)
	{
		var options = unit.Movement.GetPreviews(unit.State, grid);
		if (options.Count == 0)
			return null;

		Option? best = null;
		var bestScore = int.MinValue;
		var currentlyInHazard = hazardCells.Contains(unit.State.Position);

		foreach (var option in options)
		{
			var score = ScoreOption(unit.State, option, hazardCells, currentlyInHazard);
			if (score <= bestScore)
				continue;

			bestScore = score;
			best = option;
		}

		return best;
	}

	private static int ScoreOption(
		State state,
		Option option,
		IReadOnlySet<Coord> hazardCells,
		bool currentlyInHazard)
	{
		var score = 0;
		var end = option.EndPosition;
		var endsInHazard = hazardCells.Contains(end);

		if (endsInHazard)
			score -= 10_000;

		if (currentlyInHazard && !endsInHazard)
			score += 5_000;

		var projectedMomentum = ProjectMomentum(state, option.Path);
		score += projectedMomentum * 200;
		score += CountForwardSteps(state, option.Path) * 25;
		score -= option.ApCost;

		return score;
	}

	private static int ProjectMomentum(State state, IReadOnlyList<Coord> path)
	{
		var momentum = state.MomentumLevel;
		var pos = state.Position;

		foreach (var next in path)
		{
			var delta = next - pos;
			if (delta == state.ForwardDirection)
				momentum = System.Math.Min(momentum + 1, MomentumConfig.MaxLevel);
			else if (delta == Coord.Zero - state.ForwardDirection)
				momentum = System.Math.Max(momentum - 1, 0);

			pos = next;
		}

		return momentum;
	}

	private static int CountForwardSteps(State state, IReadOnlyList<Coord> path)
	{
		var count = 0;
		var pos = state.Position;

		foreach (var next in path)
		{
			if (next - pos == state.ForwardDirection)
				count++;

			pos = next;
		}

		return count;
	}
}
