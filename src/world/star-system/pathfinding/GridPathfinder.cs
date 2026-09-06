using GrimSpace.Math.Grid;

namespace GrimSpace.World.StarSystem.Pathfinding;

public sealed class GridPathfinder : IPathfinder
{
	private static readonly (int Dx, int Dz, bool Diagonal)[] Directions =
	[
		(0, -1, false),
		(1, 0, false),
		(0, 1, false),
		(-1, 0, false),
		(-1, -1, true),
		(1, -1, true),
		(1, 1, true),
		(-1, 1, true),
	];

	private const double Sqrt2 = 1.4142135623730951;
	private const double OctileDiagonalFactor = Sqrt2 - 2.0;

	private readonly PathfindingTerrain _terrain;

	public GridPathfinder(PathfindingTerrain terrain) => _terrain = terrain;

	public PathfindingResult FindPath(Coord origin, Coord destination)
	{
		if (!IsTraversable(origin) || !IsTraversable(destination))
			return new PathfindingResult.Unreachable();

		var path = FindGridPath(origin, destination);
		if (path.Count == 0)
			return new PathfindingResult.Unreachable();

		var points = new List<Coord>(path.Count);
		var speedMultipliers = new List<double>(path.Count);
		foreach (var coord in path)
		{
			points.Add(coord);
			speedMultipliers.Add(_terrain.CellAt(coord).SpeedMultiplier);
		}

		return new PathfindingResult.Found(TransitPath.FromPoints(points, speedMultipliers));
	}

	private List<Coord> FindGridPath(Coord origin, Coord destination)
	{
		var open = new PriorityQueue<Coord, double>();
		var cameFrom = new Dictionary<Coord, Coord>();
		var gScore = new Dictionary<Coord, double> { [origin] = 0.0 };

		open.Enqueue(origin, OctileHeuristic(origin, destination));

		while (open.Count > 0)
		{
			var current = open.Dequeue();
			if (current == destination)
				return ReconstructPath(cameFrom, current);

			foreach (var (neighbor, stepCost) in Neighbors(current))
			{
				var tentative = gScore[current] + stepCost;
				if (gScore.TryGetValue(neighbor, out var existing) && tentative >= existing)
					continue;

				cameFrom[neighbor] = current;
				gScore[neighbor] = tentative;
				open.Enqueue(neighbor, tentative + OctileHeuristic(neighbor, destination));
			}
		}

		return [];
	}

	private IEnumerable<(Coord Neighbor, double Cost)> Neighbors(Coord cell)
	{
		foreach (var (dx, dz, diagonal) in Directions)
		{
			var neighbor = new Coord(cell.X + dx, 0, cell.Z + dz);
			if (!IsTraversable(neighbor))
				continue;

			if (diagonal
				&& (!IsTraversable(new Coord(cell.X + dx, 0, cell.Z))
					|| !IsTraversable(new Coord(cell.X, 0, cell.Z + dz))))
				continue;

			var weight = _terrain.CellAt(neighbor).WeightScale;
			yield return (neighbor, diagonal ? weight * Sqrt2 : weight);
		}
	}

	private static List<Coord> ReconstructPath(IReadOnlyDictionary<Coord, Coord> cameFrom, Coord current)
	{
		var path = new List<Coord> { current };
		while (cameFrom.TryGetValue(current, out var previous))
		{
			current = previous;
			path.Add(current);
		}

		path.Reverse();
		return path;
	}

	private static double OctileHeuristic(Coord from, Coord to)
	{
		var dx = System.Math.Abs(from.X - to.X);
		var dz = System.Math.Abs(from.Z - to.Z);
		return dx + dz + OctileDiagonalFactor * System.Math.Min(dx, dz);
	}

	private bool IsTraversable(Coord coord) =>
		coord.X >= 0
		&& coord.Z >= 0
		&& coord.X < _terrain.Width
		&& coord.Z < _terrain.Height
		&& !_terrain.CellAt(coord).Blocked;
}
