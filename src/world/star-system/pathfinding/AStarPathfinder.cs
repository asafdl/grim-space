using Godot;
using GrimSpace.Math.Grid;

namespace GrimSpace.World.StarSystem.Pathfinding;

public sealed class AStarPathfinder : IPathfinder
{
	private readonly PathfindingTerrain _terrain;
	private readonly AStarGrid2D _grid;

	public AStarPathfinder(PathfindingTerrain terrain)
	{
		_terrain = terrain;
		_grid = new AStarGrid2D
		{
			Region = new Rect2I(0, 0, terrain.Width, terrain.Height),
			CellSize = Vector2.One,
			Offset = Vector2.Zero,
			DefaultComputeHeuristic = AStarGrid2D.Heuristic.Manhattan,
			DefaultEstimateHeuristic = AStarGrid2D.Heuristic.Manhattan,
		};
		_grid.DiagonalMode = AStarGrid2D.DiagonalModeEnum.Never;
		_grid.Update();

		for (var z = 0; z < terrain.Height; z++)
		{
			for (var x = 0; x < terrain.Width; x++)
			{
				var cell = terrain[x, z];
				var id = new Vector2I(x, z);
				_grid.SetPointSolid(id, cell.Blocked);
				if (!cell.Blocked)
					_grid.SetPointWeightScale(id, (float)cell.WeightScale);
			}
		}
	}

	public PathfindingResult FindPath(Coord origin, Coord destination)
	{
		if (!IsTraversable(origin) || !IsTraversable(destination))
			return new PathfindingResult.Unreachable();

		var path = _grid.GetIdPath(origin.ToVector2I(), destination.ToVector2I());
		if (path.Count == 0)
			return new PathfindingResult.Unreachable();

		var points = new List<Coord>(path.Count);
		var speedMultipliers = new List<double>(path.Count);
		foreach (Vector2I cell in path)
		{
			var coord = Coord.FromVector2I(cell);
			points.Add(coord);
			speedMultipliers.Add(_terrain.CellAt(coord).SpeedMultiplier);
		}

		return new PathfindingResult.Found(TransitPath.FromPoints(points, speedMultipliers));
	}

	private bool IsTraversable(Coord coord) =>
		coord.X >= 0
		&& coord.Z >= 0
		&& coord.X < _terrain.Width
		&& coord.Z < _terrain.Height
		&& !_terrain.CellAt(coord).Blocked;
}
