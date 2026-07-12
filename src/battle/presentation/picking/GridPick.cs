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

		for (var t = 0f; t <= grid.Width * WorldMapping.CellSize * 2f; t += 0.5f)
		{
			var point = origin + direction * t;
			var cell = WorldToCell(point);

			if (!grid.IsInBounds(cell))
				continue;

			var center = WorldMapping.ToWorld(cell);
			var distance = center.DistanceTo(point);
			if (distance >= WorldMapping.CellSize * 0.75f || distance >= bestDistance)
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
			var center = WorldMapping.ToWorld(cell);
			var distance = DistanceRayToPoint(origin, direction, center);
			if (distance >= WorldMapping.CellSize || distance >= bestDistance)
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
			var world = WorldMapping.ToWorld(unit.State.Position);
			var distance = DistanceRayToPoint(origin, direction, world);
			if (distance >= bestDistance)
				continue;

			bestDistance = distance;
			best = unit;
		}

		return best;
	}

	private static Coord WorldToCell(Vector3 world)
	{
		var x = Mathf.FloorToInt(world.X / WorldMapping.CellSize);
		var y = Mathf.FloorToInt(world.Y / WorldMapping.CellSize);
		var z = Mathf.FloorToInt(world.Z / WorldMapping.CellSize);
		return new Coord(x, y, z);
	}

	private static float DistanceRayToPoint(Vector3 origin, Vector3 direction, Vector3 point)
	{
		var toPoint = point - origin;
		var t = Mathf.Clamp(toPoint.Dot(direction), 0f, 400f);
		var closest = origin + direction * t;
		return closest.DistanceTo(point);
	}
}
