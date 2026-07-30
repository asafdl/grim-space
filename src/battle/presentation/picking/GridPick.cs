using Godot;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Units;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Presentation.Picking;

public static class GridPick
{
	private const float UnitPickRadius = 2f;

	public static Coord? PickCell(Camera3D camera, Vector2 screenPos, BoundedGrid grid)
	{
		var origin = camera.ProjectRayOrigin(screenPos);
		var direction = camera.ProjectRayNormal(screenPos);

		Coord? best = null;
		var bestDistance = float.MaxValue;

		for (var t = 0f; t <= grid.Width * PointMapping.CellSize * 2f; t += 0.5f)
		{
			var point = origin + direction * t;
			var cell = PointMapping.ToCoord(point);

			if (!grid.IsInBounds(cell))
				continue;

			var center = PointMapping.ToWorld(cell);
			var distance = center.DistanceTo(point);
			if (distance >= PointMapping.CellSize * 0.75f || distance >= bestDistance)
				continue;

			bestDistance = distance;
			best = cell;
		}

		return best;
	}

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
			var center = PointMapping.ToWorld(cell);
			var distance = DistanceRayToPoint(origin, direction, center);
			if (distance >= PointMapping.CellSize || distance >= bestDistance)
				continue;

			bestDistance = distance;
			best = cell;
		}

		return best;
	}

	public static Unit? PickUnit(Camera3D camera, Vector2 screenPos, IReadOnlyList<Unit> units)
	{
		var origin = camera.ProjectRayOrigin(screenPos);
		var direction = camera.ProjectRayNormal(screenPos);

		Unit? best = null;
		var bestDistance = UnitPickRadius;

		foreach (var unit in units)
		{
			var world = PointMapping.ToWorld(unit.State.Position);
			var distance = DistanceRayToPoint(origin, direction, world);
			if (distance >= bestDistance)
				continue;

			bestDistance = distance;
			best = unit;
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
