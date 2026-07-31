using Godot;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Presentation.Picking;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Ui;

public static class MovementSelection
{
	public static string FormatMomentum(State unit)
	{
		var config = MomentumConfig.ForLevel(unit.MomentumLevel);
		var evasion = (int)(config.Evasion * 100);
		return $"M{unit.MomentumLevel} ({evasion}% eva)";
	}

	private const float PickRadius = 1.4f;

	public static int? PickOptionIndex(Camera3D camera, Vector2 screenPos, IReadOnlyList<Option> options)
	{
		if (options.Count == 0)
			return null;

		var origin = camera.ProjectRayOrigin(screenPos);
		var direction = camera.ProjectRayNormal(screenPos);

		int? bestIndex = null;
		var bestDistance = PickRadius;

		for (var i = 0; i < options.Count; i++)
		{
			var world = PointMapping.ToWorld(options[i].EndPosition);
			var distance = DistanceRayToPoint(origin, direction, world);
			if (distance >= bestDistance)
				continue;

			bestDistance = distance;
			bestIndex = i;
		}

		return bestIndex;
	}

	public static int? PickOptionIndexOnRing(
		Camera3D camera,
		Vector2 screenPos,
		IReadOnlyList<Option> options,
		IReadOnlyList<int> optionIndicesOnRing)
	{
		if (optionIndicesOnRing.Count == 0)
			return null;

		var cells = new HashSet<Coord>();
		foreach (var index in optionIndicesOnRing)
			cells.Add(options[index].EndPosition);

		var picked = GridPick.PickFromSet(camera, screenPos, cells);
		if (picked is not Coord cell)
			return null;

		foreach (var index in optionIndicesOnRing)
		{
			if (options[index].EndPosition == cell)
				return index;
		}

		return null;
	}

	private static float DistanceRayToPoint(Vector3 origin, Vector3 direction, Vector3 point)
	{
		var toPoint = point - origin;
		var t = Mathf.Clamp(toPoint.Dot(direction), 0f, 200f);
		var closest = origin + direction * t;
		return closest.DistanceTo(point);
	}
}
