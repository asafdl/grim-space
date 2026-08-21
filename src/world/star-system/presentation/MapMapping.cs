using Godot;
using GrimSpace.Math.Grid;

namespace GrimSpace.World.StarSystem.Presentation;

public static class MapMapping
{
	public const float WorldUnitsPerPoint = 1f / 32f;

	public static Vector3 ToWorld(Coord point, int width, int height)
	{
		var origin = GridOrigin(width, height);
		return origin + new Vector3(
			point.X * WorldUnitsPerPoint,
			0f,
			point.Z * WorldUnitsPerPoint);
	}

	public static Vector3 GridOrigin(int width, int height) =>
		new(-width * WorldUnitsPerPoint * 0.5f, 0f, -height * WorldUnitsPerPoint * 0.5f);

	public static Coord? FromWorld(Vector3 point, int width, int height)
	{
		var local = point - GridOrigin(width, height);
		var x = Mathf.RoundToInt(local.X / WorldUnitsPerPoint);
		var z = Mathf.RoundToInt(local.Z / WorldUnitsPerPoint);
		if (x < 0 || x >= width || z < 0 || z >= height)
			return null;
		return new Coord(x, 0, z);
	}
}
