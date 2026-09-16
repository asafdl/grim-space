using Godot;

namespace GrimSpace.World.StarSystem.Presentation;

public static class MapScreenAnchor
{
	public static bool TryProject(
		Camera3D camera,
		Vector3 worldPosition,
		out Vector2 screenPosition)
	{
		if (camera.IsPositionBehind(worldPosition))
		{
			screenPosition = default;
			return false;
		}

		screenPosition = camera.UnprojectPosition(worldPosition);
		return camera.GetViewport().GetVisibleRect().HasPoint(screenPosition);
	}

	public static Vector2 TopLeftForControl(Vector2 anchorScreen, Vector2 controlSize, Vector2 pixelOffset) =>
		anchorScreen + pixelOffset - new Vector2(0f, controlSize.Y * 0.5f);
}
