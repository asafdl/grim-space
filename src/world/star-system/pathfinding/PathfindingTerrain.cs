using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Traffic;

namespace GrimSpace.World.StarSystem.Pathfinding;

public sealed class PathfindingTerrain
{
	public int Width { get; }
	public int Height { get; }
	private readonly PathfindingCell[] _cells;

	private PathfindingTerrain(int width, int height, PathfindingCell[] cells)
	{
		Width = width;
		Height = height;
		_cells = cells;
	}

	public PathfindingCell this[int x, int z] => _cells[new Coord(x, 0, z).ToIndex(Width)];

	public PathfindingCell CellAt(Coord coord) => this[coord.X, coord.Z];

	public static PathfindingTerrain FromCells(int width, int height, PathfindingCell[] cells)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
		ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
		if (cells.Length != width * height)
		{
			throw new ArgumentException(
				$"Cell buffer length {cells.Length} does not match map size {width}x{height}.",
				nameof(cells));
		}

		return new PathfindingTerrain(width, height, cells);
	}

	public static PathfindingTerrain Create(
		int width,
		int height,
		IEnumerable<SpaceRoute> routes,
		IReadOnlyCollection<PointOfInterest> pois,
		IEnumerable<Dock> docks)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
		ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);

		var cells = Enumerable.Repeat(PathfindingCell.OpenSpace, width * height).ToArray();

		foreach (var route in routes)
		{
			GridRaster.StampCorridor(
				width,
				height,
				route.Centerline,
				route.HalfWidth,
				(x, z) => cells[new Coord(x, 0, z).ToIndex(width)] = PathfindingCell.RouteCorridor);
		}

		foreach (var poi in pois)
		{
			GridRaster.FillCircle(
				width,
				height,
				poi.PlacedCenter,
				poi.RouteExclusionRadius,
				(x, z) => cells[new Coord(x, 0, z).ToIndex(width)] = PathfindingCell.Obstacle);
		}

		foreach (var dock in docks)
		{
			var index = dock.Position.ToIndex(width);
			if (index < 0 || index >= cells.Length)
				continue;

			cells[index] = PathfindingCell.OpenSpace;
		}

		return new PathfindingTerrain(width, height, cells);
	}
}
