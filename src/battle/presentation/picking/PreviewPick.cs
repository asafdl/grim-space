using Godot;

namespace GrimSpace.Battle.Presentation.Picking;

public static class PreviewPick
{
	private const float PickRadiusPx = 80f;

	public static bool NearSegment(Camera3D camera, Vector2 screenPos, Vector3 from, Vector3 to) =>
		!camera.IsPositionBehind(from)
		&& !camera.IsPositionBehind(to)
		&& DistanceToSegment(
			screenPos,
			camera.UnprojectPosition(from),
			camera.UnprojectPosition(to)) <= PickRadiusPx;

	internal static float DistanceToSegment(Vector2 point, Vector2 from, Vector2 to)
	{
		var segment = to - from;
		var lengthSquared = segment.LengthSquared();
		if (lengthSquared <= float.Epsilon)
			return point.DistanceTo(from);

		var t = Mathf.Clamp((point - from).Dot(segment) / lengthSquared, 0f, 1f);
		return point.DistanceTo(from + segment * t);
	}
}
