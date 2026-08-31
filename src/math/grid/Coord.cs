using System;
using System.Collections.Generic;
using Godot;

namespace GrimSpace.Math.Grid;

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

	public static int Dot(Coord a, Coord b) =>
		a.X * b.X + a.Y * b.Y + a.Z * b.Z;

	public static IEnumerable<Coord> OffsetsInCube(int radius)
	{
		for (var dx = -radius; dx <= radius; dx++)
		{
			for (var dy = -radius; dy <= radius; dy++)
			{
				for (var dz = -radius; dz <= radius; dz++)
					yield return new Coord(dx, dy, dz);
			}
		}
	}

	public int ToIndex(int width) => Z * width + X;

	public static Coord FromIndex(int index, int width) =>
		new(index % width, 0, index / width);

	public Vector2I ToVector2I() => new(X, Z);

	public static Coord FromVector2I(Vector2I cell) => new(cell.X, 0, cell.Y);

	public int ManhattanDistanceTo(Coord other) =>
		System.Math.Abs(X - other.X) + System.Math.Abs(Y - other.Y) + System.Math.Abs(Z - other.Z);

	public override string ToString() => $"({X},{Y},{Z})";
}
