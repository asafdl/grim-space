using Godot;

namespace GrimSpace.Math.Camera;

public static class CameraTransition
{
	public const float MinDuration = 0.35f;
	public const float MaxDuration = 1.2f;

	private const float PivotUnitsPerSecond = 24f;
	private const float ZoomRatioPerSecond = 0.8f;
	private const float RadiansPerSecond = 2.1f;
	private const float DistanceEpsilon = 0.001f;

	public static float Duration(OrbitPose from, OrbitPose to)
	{
		var pivotDuration = from.Pivot.DistanceTo(to.Pivot) / PivotUnitsPerSecond;
		var fromDistance = Mathf.Max(from.Distance, DistanceEpsilon);
		var toDistance = Mathf.Max(to.Distance, DistanceEpsilon);
		var zoomDuration =
			Mathf.Abs(Mathf.Log(toDistance / fromDistance)) / ZoomRatioPerSecond;
		var yawDuration =
			Mathf.Abs(Mathf.AngleDifference(from.Yaw, to.Yaw)) / RadiansPerSecond;
		var pitchDuration = Mathf.Abs(to.Pitch - from.Pitch) / RadiansPerSecond;

		return Mathf.Clamp(
			Mathf.Max(
				pivotDuration,
				Mathf.Max(zoomDuration, Mathf.Max(yawDuration, pitchDuration))),
			MinDuration,
			MaxDuration);
	}
}
