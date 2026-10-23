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
	private const double MinWeightScale = 1.0;

	private readonly PathfindingTerrain _terrain;
	private readonly int _width;
	private readonly int _cellCount;
	private double[] _gScore = [];
	private int[] _cameFrom = [];
	private byte[] _closed = [];

	public GridPathfinder(PathfindingTerrain terrain)
	{
		_terrain = terrain;
		_width = terrain.Width;
		_cellCount = _width * terrain.Height;
	}

	public PathfindingResult FindPath(Coord origin, Coord destination)
	{
		if (!_terrain.IsTraversable(origin) || !_terrain.IsTraversable(destination))
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
		EnsureSearchBuffers();
		Array.Fill(_gScore, double.PositiveInfinity);
		Array.Clear(_closed, 0, _cellCount);

		var originIdx = origin.ToIndex(_width);
		var destIdx = destination.ToIndex(_width);
		var destX = destination.X;
		var destZ = destination.Z;

		_gScore[originIdx] = 0.0;
		_cameFrom[originIdx] = originIdx;

		var open = new PriorityQueue<int, double>();
		open.Enqueue(originIdx, OctileHeuristic(origin.X, origin.Z, destX, destZ));

		while (open.Count > 0)
		{
			var currentIdx = open.Dequeue();
			if (_closed[currentIdx] != 0)
				continue;

			_closed[currentIdx] = 1;

			if (currentIdx == destIdx)
				return ReconstructPath(originIdx, destIdx);

			var cx = currentIdx % _width;
			var cz = currentIdx / _width;
			var currentG = _gScore[currentIdx];

			foreach (var (dx, dz, diagonal) in Directions)
			{
				var nx = cx + dx;
				var nz = cz + dz;
				if (!_terrain.IsTraversable(nx, nz))
					continue;

				if (diagonal
					&& (!_terrain.IsTraversable(cx + dx, cz)
						|| !_terrain.IsTraversable(cx, cz + dz)))
					continue;

				var neighborIdx = nz * _width + nx;
				if (_closed[neighborIdx] != 0)
					continue;

				var weight = _terrain.CellAtIndex(neighborIdx).WeightScale;
				var stepCost = diagonal ? weight * Sqrt2 : weight;
				var tentative = currentG + stepCost;
				if (tentative >= _gScore[neighborIdx])
					continue;

				_gScore[neighborIdx] = tentative;
				_cameFrom[neighborIdx] = currentIdx;
				open.Enqueue(
					neighborIdx,
					tentative + OctileHeuristic(nx, nz, destX, destZ));
			}
		}

		return [];
	}

	private void EnsureSearchBuffers()
	{
		if (_gScore.Length >= _cellCount)
			return;

		_gScore = new double[_cellCount];
		_cameFrom = new int[_cellCount];
		_closed = new byte[_cellCount];
	}

	private List<Coord> ReconstructPath(int originIdx, int destIdx)
	{
		var path = new List<Coord>();
		var idx = destIdx;
		while (idx != originIdx)
		{
			path.Add(Coord.FromIndex(idx, _width));
			idx = _cameFrom[idx];
		}

		path.Add(Coord.FromIndex(originIdx, _width));
		path.Reverse();
		return path;
	}

	private static double OctileHeuristic(int fromX, int fromZ, int toX, int toZ)
	{
		var dx = System.Math.Abs(fromX - toX);
		var dz = System.Math.Abs(fromZ - toZ);
		return MinWeightScale * (dx + dz + OctileDiagonalFactor * System.Math.Min(dx, dz));
	}
}
