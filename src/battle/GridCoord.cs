using System;

namespace GrimSpace.Battle;

public readonly record struct GridCoord(int X, int Y, int Z)
{
	public int ManhattanDistanceTo(GridCoord other) =>
		Math.Abs(X - other.X) + Math.Abs(Y - other.Y) + Math.Abs(Z - other.Z);
}
