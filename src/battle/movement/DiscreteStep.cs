using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Units;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Movement;

public sealed class DiscreteStep : IMovement
{
	public const int MinMoveApCost = 3;

	private static readonly EStepDirection[] Directions = Enum.GetValues<EStepDirection>();

	public IReadOnlyList<Option> GetMoveOptions(State unit, BoundedGrid grid, IReadOnlySet<Coord> blockedCells)
	{
		var byEndpoint = new Dictionary<Coord, Option>();
		var visited = new Dictionary<SearchNode, int>();
		Search(
			new SearchNode(unit.Position, UsedDirectionsMask: 0, unit.MomentumLevel),
			unit.ActionPoints,
			0,
			unit,
			grid,
			blockedCells,
			[],
			byEndpoint,
			visited);
		return byEndpoint.Values.ToList();
	}

	public bool CanMove(State unit, Option option) =>
		option.Path.Count > 0
		&& (option.ApCost == 0 || option.ApCost >= MinMoveApCost)
		&& unit.ActionPoints >= option.ApCost
		&& !PathUsesOpposingDirections(unit, unit.Position, option.Path);

	public void ApplyMove(State unit, Option option) =>
		unit.Position = option.EndPosition;

	public void ApplyMomentum(State unit, IReadOnlyList<Coord> path) =>
		ApplyMomentumFromPath(unit, path);

	private readonly record struct SearchNode(Coord Position, int UsedDirectionsMask, int MomentumLevel);

	private static void Search(
		SearchNode node,
		int apRemaining,
		int apSpent,
		State unit,
		BoundedGrid grid,
		IReadOnlySet<Coord> blockedCells,
		List<Coord> pathSoFar,
		Dictionary<Coord, Option> results,
		Dictionary<SearchNode, int> visited)
	{
		if (apRemaining <= 0)
			return;

		if (visited.TryGetValue(node, out var seenAp) && seenAp >= apRemaining)
			return;

		visited[node] = apRemaining;

		var frame = BodyFrame.From(unit);

		foreach (var direction in Directions)
		{
			if (UsesOpposite(node.UsedDirectionsMask, direction))
				continue;

			var forwardStepsInPath = CountForwardSteps(frame, pathSoFar, unit.Position);
			var stepCost = StepCosts.GetMoveStepApCost(
				direction,
				new MoveStepContext(forwardStepsInPath, node.MomentumLevel));
			if (stepCost > apRemaining)
				continue;

			var next = node.Position + frame.Step(direction);
			if (!grid.IsInBounds(next) || blockedCells.Contains(next))
				continue;

			var fullPath = new List<Coord>(pathSoFar) { next };
			var totalAp = apSpent + stepCost;
			var nextMomentum = node.MomentumLevel;
			if (direction == EStepDirection.Forward)
				nextMomentum = System.Math.Min(nextMomentum + 1, MomentumConfig.MaxLevel);
			else if (direction == EStepDirection.Retro)
				nextMomentum = System.Math.Max(nextMomentum - 1, 0);

			var nextNode = new SearchNode(
				next,
				node.UsedDirectionsMask | DirectionBit(direction),
				nextMomentum);

			if (totalAp >= MinMoveApCost || totalAp == 0)
			{
				if (!results.TryGetValue(next, out var existing) || totalAp < existing.ApCost)
				{
					results[next] = new Option
					{
						ApCost = totalAp,
						Path = fullPath,
					};
				}
			}

			Search(nextNode, apRemaining - stepCost, totalAp, unit, grid, blockedCells, fullPath, results, visited);
		}
	}

	private static bool PathUsesOpposingDirections(State unit, Coord origin, IReadOnlyList<Coord> path)
	{
		var frame = BodyFrame.From(unit);
		var usedMask = 0;
		var pos = origin;

		foreach (var next in path)
		{
			if (frame.DirectionOfStep(pos, next) is not EStepDirection direction)
				return true;

			if (UsesOpposite(usedMask, direction))
				return true;

			usedMask |= DirectionBit(direction);
			pos = next;
		}

		return false;
	}

	private static int DirectionBit(EStepDirection direction) => 1 << (int)direction;

	private static bool UsesOpposite(int usedMask, EStepDirection direction) =>
		(usedMask & DirectionBit(Opposite(direction))) != 0;

	private static EStepDirection Opposite(EStepDirection direction) =>
		direction switch
		{
			EStepDirection.Forward => EStepDirection.Retro,
			EStepDirection.Retro => EStepDirection.Forward,
			EStepDirection.Dorsal => EStepDirection.Ventral,
			EStepDirection.Ventral => EStepDirection.Dorsal,
			EStepDirection.Port => EStepDirection.Starboard,
			EStepDirection.Starboard => EStepDirection.Port,
			_ => direction,
		};

	private static int CountForwardSteps(BodyFrame frame, IReadOnlyList<Coord> pathSoFar, Coord origin)
	{
		if (pathSoFar.Count == 0)
			return 0;

		var pos = origin;
		var count = 0;

		foreach (var next in pathSoFar)
		{
			if (frame.DirectionOfStep(pos, next) == EStepDirection.Forward)
				count++;

			pos = next;
		}

		return count;
	}

	private static void ApplyMomentumFromPath(State unit, IReadOnlyList<Coord> path)
	{
		var frame = BodyFrame.From(unit);
		var pos = unit.Position;

		foreach (var next in path)
		{
			var direction = frame.DirectionOfStep(pos, next);
			if (direction == EStepDirection.Forward)
				unit.MomentumLevel = System.Math.Min(unit.MomentumLevel + 1, MomentumConfig.MaxLevel);
			else if (direction == EStepDirection.Retro)
				unit.MomentumLevel = System.Math.Max(unit.MomentumLevel - 1, 0);

			pos = next;
		}
	}
}
