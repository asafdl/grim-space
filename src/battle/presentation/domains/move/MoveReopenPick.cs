using Godot;
using GrimSpace.Battle.Presentation;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Domains.Move;

internal static class MoveReopenPick
{
	internal const float CellPickRadiusPx = 64f;

	public static bool MatchesCell(
		Camera3D camera,
		Viewport viewport,
		Vector2 screenPos,
		Coord cell)
	{
		var viewportRect = viewport.GetVisibleRect();
		if (!viewportRect.HasPoint(screenPos))
			return false;

		var centerWorld = WorldMapping.ToWorld(cell);
		if (camera.IsPositionBehind(centerWorld))
			return false;

		var depth = CameraForwardDepth(camera, centerWorld);
		if (depth <= camera.Near)
			return false;

		var projected = camera.UnprojectPosition(centerWorld);
		if (!IsFinite(projected))
			return false;

		return projected.DistanceTo(screenPos) <= CellPickRadiusPx;
	}

	private static float CameraForwardDepth(Camera3D camera, Vector3 world) =>
		-(camera.GlobalTransform.AffineInverse() * world).Z;

	private static bool IsFinite(Vector2 value) =>
		float.IsFinite(value.X) && float.IsFinite(value.Y);
}
