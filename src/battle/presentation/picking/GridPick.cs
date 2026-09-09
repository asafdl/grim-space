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
		var bestDepth = float.MaxValue;
		var bestOffset = float.MaxValue;

		foreach (var cell in validCells)
		{
			var center = WorldMapping.ToWorld(cell);
			var toCenter = center - origin;
			var depth = toCenter.Dot(direction);
			if (depth < 0f)
				continue;

			var offset = (origin + direction * depth).DistanceTo(center);
			if (offset >= WorldMapping.CellSize
				|| !IsBetter(depth, offset, bestDepth, bestOffset))
			{
				continue;
			}

			bestDepth = depth;
			bestOffset = offset;
			best = cell;
		}

		return best;
	}

	internal static bool IsBetter(float depth, float offset, float bestDepth, float bestOffset) =>
		depth < bestDepth || Mathf.IsEqualApprox(depth, bestDepth) && offset < bestOffset;
}
