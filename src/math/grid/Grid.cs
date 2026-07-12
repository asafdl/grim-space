using System.Collections.Generic;

namespace GrimSpace.Math.Grid;

public sealed class Grid
{
	public int Width { get; }
	public int Height { get; }
	public int Depth { get; }

	public Grid(int width, int height, int depth)
	{
		if (width < 1 || height < 1 || depth < 1)
			throw new ArgumentOutOfRangeException(nameof(width), "Grid must be at least 1x1x1.");

		Width = width;
		Height = height;
		Depth = depth;
	}

	public bool IsInBounds(Coord coord) =>
		coord.X >= 0 && coord.X < Width
		&& coord.Y >= 0 && coord.Y < Height
		&& coord.Z >= 0 && coord.Z < Depth;

	public IEnumerable<Coord> EnumerateCube(Coord center, int radius)
	{
		foreach (var offset in Coord.OffsetsInCube(radius))
		{
			var cell = center + offset;
			if (IsInBounds(cell))
				yield return cell;
		}
	}
}
