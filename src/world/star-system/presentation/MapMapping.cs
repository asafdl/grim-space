using Godot;
using GrimSpace.Math.Grid;

namespace GrimSpace.World.StarSystem.Presentation;

public static class MapMapping
{
	public const float CellSize = 1f;

	public static Vector3 ToWorld(Coord cell, int width, int height) =>
		CellOrigin(cell, width, height) + new Vector3(CellSize * 0.5f, 0f, CellSize * 0.5f);

	public static Vector3 CellOrigin(Coord cell, int width, int height) =>
		new(
			(cell.X - width * 0.5f) * CellSize,
			0f,
			(cell.Z - height * 0.5f) * CellSize);

	public static Vector3 GridOrigin(int width, int height) =>
		new(-width * CellSize * 0.5f, 0f, -height * CellSize * 0.5f);

	public static Coord? FromWorld(Vector3 point, int width, int height)
	{
		var local = point - GridOrigin(width, height);
		var x = Mathf.FloorToInt(local.X / CellSize);
		var z = Mathf.FloorToInt(local.Z / CellSize);
		if (x < 0 || x >= width || z < 0 || z >= height)
			return null;
		return new Coord(x, 0, z);
	}
}
