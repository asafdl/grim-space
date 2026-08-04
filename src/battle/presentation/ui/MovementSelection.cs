using Godot;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Units;

namespace GrimSpace.Battle.Presentation.Ui;

public static class MovementSelection
{
	public static string FormatMomentum(State unit) => $"M{unit.MomentumLevel}";

	private const float PickRadius = 1.4f;

	public static int? PickPathIndex(Camera3D camera, Vector2 screenPos, IReadOnlyList<MovePathSession> paths)
	{
		if (paths.Count == 0)
			return null;

		var origin = camera.ProjectRayOrigin(screenPos);
		var direction = camera.ProjectRayNormal(screenPos);

		int? bestIndex = null;
		var bestDistance = PickRadius;

		for (var i = 0; i < paths.Count; i++)
		{
			var world = WorldMapping.ToWorld(paths[i].EndPosition);
			var distance = DistanceRayToPoint(origin, direction, world);
			if (distance >= bestDistance)
				continue;

			bestDistance = distance;
			bestIndex = i;
		}

		return bestIndex;
	}

	private static float DistanceRayToPoint(Vector3 origin, Vector3 direction, Vector3 point)
	{
		var toPoint = point - origin;
		var t = Mathf.Clamp(toPoint.Dot(direction), 0f, 200f);
		var closest = origin + direction * t;
		return closest.DistanceTo(point);
	}
}
