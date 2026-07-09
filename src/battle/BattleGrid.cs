using System;

namespace GrimSpace.Battle;

public sealed class BattleGrid
{
	public int Width { get; }
	public int Height { get; }
	public int Depth { get; }

	public BattleGrid(int width, int height, int depth)
	{
		if (width < 1 || height < 1 || depth < 1)
			throw new ArgumentOutOfRangeException(nameof(width), "Grid must be at least 1x1x1.");

		Width = width;
		Height = height;
		Depth = depth;
	}

	public bool IsInBounds(GridCoord coord) =>
		coord.X >= 0 && coord.X < Width
		&& coord.Y >= 0 && coord.Y < Height
		&& coord.Z >= 0 && coord.Z < Depth;
}
