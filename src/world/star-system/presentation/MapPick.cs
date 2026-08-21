using Godot;
using GrimSpace.Math.Grid;

namespace GrimSpace.World.StarSystem.Presentation;

public static class MapPick
{
	public static Coord? PickPoint(Camera3D camera, Vector2 screenPos, int width, int height)
	{
		var origin = camera.ProjectRayOrigin(screenPos);
		var direction = camera.ProjectRayNormal(screenPos);
		if (Mathf.Abs(direction.Y) < 0.0001f)
			return null;

		var t = -origin.Y / direction.Y;
		if (t < 0f)
			return null;

		return MapMapping.FromWorld(origin + direction * t, width, height);
	}
}
