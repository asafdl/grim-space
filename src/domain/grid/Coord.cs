using System;

namespace GrimSpace.Domain.Grid;

public readonly record struct Coord(int X, int Y, int Z)
{
	public static Coord Zero => new(0, 0, 0);
	public static Coord Forward => new(0, 0, 1);
	public static Coord Up => new(0, 1, 0);

	public static Coord operator +(Coord a, Coord b) =>
		new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

	public static Coord operator -(Coord a, Coord b) =>
		new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

	public static Coord operator -(Coord a) =>
		new(-a.X, -a.Y, -a.Z);

	public static Coord operator *(Coord a, int multiplier) =>
		new(a.X * multiplier, a.Y * multiplier, a.Z * multiplier);

	public static Coord Cross(Coord a, Coord b) =>
		new(
			a.Y * b.Z - a.Z * b.Y,
			a.Z * b.X - a.X * b.Z,
			a.X * b.Y - a.Y * b.X);

	public int ManhattanDistanceTo(Coord other) =>
		Math.Abs(X - other.X) + Math.Abs(Y - other.Y) + Math.Abs(Z - other.Z);
}
