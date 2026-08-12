using Godot;

namespace GrimSpace.Battle.Presentation.Picking;

public static class PreviewPick
{
	private const float PickRadiusPx = 80f;

	public static bool NearNode(Camera3D camera, Vector2 screenPos, Node3D node) =>
		node.Visible
		&& !camera.IsPositionBehind(node.GlobalPosition)
		&& camera.UnprojectPosition(node.GlobalPosition).DistanceTo(screenPos) <= PickRadiusPx;

	public static float ScreenDistance(Camera3D camera, Vector2 screenPos, Node3D node) =>
		camera.UnprojectPosition(node.GlobalPosition).DistanceTo(screenPos);
}
