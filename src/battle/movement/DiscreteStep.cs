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

		foreach (var direction in Directions)
		{
			if (UsesOpposite(node.UsedDirectionsMask, direction))
				continue;

			var forwardStepsInPath = CountForwardSteps(pathSoFar, unit);
			var stepCost = StepCosts.GetMoveStepApCost(
				direction,
				new MoveStepContext(forwardStepsInPath, node.MomentumLevel));
			if (stepCost > apRemaining)
				continue;

			var next = node.Position + StepDelta(unit, direction);
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
		var usedMask = 0;
		var pos = origin;

		foreach (var next in path)
		{
			if (DirectionOfStep(unit, pos, next) is not EStepDirection direction)
				return true;

			if (UsesOpposite(usedMask, direction))
				return true;

			usedMask |= DirectionBit(direction);
			pos = next;
		}

		return false;
	}

	private static EStepDirection? DirectionOfStep(State unit, Coord from, Coord to)
	{
		var delta = to - from;

		if (delta == unit.ForwardDirection)
			return EStepDirection.Forward;

		if (delta == Coord.Zero - unit.ForwardDirection)
			return EStepDirection.Retro;

		if (delta == unit.UpDirection)
			return EStepDirection.Dorsal;

		if (delta == Coord.Zero - unit.UpDirection)
			return EStepDirection.Ventral;

		if (delta == unit.RightDirection)
			return EStepDirection.Starboard;

		if (delta == Coord.Zero - unit.RightDirection)
			return EStepDirection.Port;

		return null;
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

	private static Coord StepDelta(State unit, EStepDirection direction) =>
		direction switch
		{
			EStepDirection.Forward => unit.ForwardDirection,
			EStepDirection.Retro => Coord.Zero - unit.ForwardDirection,
			EStepDirection.Dorsal => unit.UpDirection,
			EStepDirection.Ventral => Coord.Zero - unit.UpDirection,
			EStepDirection.Starboard => unit.RightDirection,
			EStepDirection.Port => Coord.Zero - unit.RightDirection,
			_ => Coord.Zero,
		};

	private static int CountForwardSteps(IReadOnlyList<Coord> pathSoFar, State unit)
	{
		if (pathSoFar.Count == 0)
			return 0;

		var pos = unit.Position;
		var count = 0;

		foreach (var next in pathSoFar)
		{
			if (DirectionOfStep(unit, pos, next) == EStepDirection.Forward)
				count++;

			pos = next;
		}

		return count;
	}

	private static void ApplyMomentumFromPath(State unit, IReadOnlyList<Coord> path)
	{
		var pos = unit.Position;

		foreach (var next in path)
		{
			var direction = DirectionOfStep(unit, pos, next);
			if (direction == EStepDirection.Forward)
				unit.MomentumLevel = System.Math.Min(unit.MomentumLevel + 1, MomentumConfig.MaxLevel);
			else if (direction == EStepDirection.Retro)
				unit.MomentumLevel = System.Math.Max(unit.MomentumLevel - 1, 0);

			pos = next;
		}
	}
}
