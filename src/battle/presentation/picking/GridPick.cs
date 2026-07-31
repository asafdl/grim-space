using Godot;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Picking;

public static class GridPick
{
	public static Coord? PickFromSet(Camera3D camera, Vector2 screenPos, IReadOnlySet<Coord> validCells)
	{
		if (validCells.Count == 0)
			return null;

		var origin = camera.ProjectRayOrigin(screenPos);
		var direction = camera.ProjectRayNormal(screenPos);

		Coord? best = null;
		var bestDistance = float.MaxValue;

		foreach (var cell in validCells)
		{
			var center = WorldMapping.ToWorld(cell);
			var distance = DistanceRayToPoint(origin, direction, center);
			if (distance >= WorldMapping.CellSize || distance >= bestDistance)
				continue;

			bestDistance = distance;
			best = cell;
		}

		return best;
	}

	private static float DistanceRayToPoint(Vector3 origin, Vector3 direction, Vector3 point)
	{
		var toPoint = point - origin;
		var t = Mathf.Clamp(toPoint.Dot(direction), 0f, 400f);
		var closest = origin + direction * t;
		return closest.DistanceTo(point);
	}
}
