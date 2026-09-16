using GrimSpace.Math.Camera;

namespace GrimSpace.World.StarSystem.Presentation;

/// <summary>
/// Top-down overview pose and limits for framing the full star map in view.
/// </summary>
public static class MapOverviewFraming
{
	private const float VerticalFovDegrees = 34f;
	private const float FramingMargin = 1.12f;

	private const float MinPitchRadians = 50f * MathF.PI / 180f;
	private const float MaxPitchRadians = 65f * MathF.PI / 180f;

	public static readonly OrbitLimits Limits = new(
		MinDistance: 24f,
		MaxDistance: 240f,
		MinPitch: MinPitchRadians,
		MaxPitch: MaxPitchRadians);

	public static OrbitPose Resolve(
		OrbitPose sourcePose,
		float boundsHalfX,
		float boundsHalfZ,
		float viewportWidth,
		float viewportHeight)
	{
		var pitch = sourcePose.Pitch;
		if (pitch < Limits.MinPitch || pitch > Limits.MaxPitch)
			pitch = (Limits.MinPitch + Limits.MaxPitch) * 0.5f;

		var distance = ComputeDistance(boundsHalfX, boundsHalfZ, viewportWidth, viewportHeight, pitch);
		distance = System.Math.Clamp(distance, Limits.MinDistance, Limits.MaxDistance);

		return new OrbitPose
		{
			Pivot = default,
			Yaw = sourcePose.Yaw,
			Pitch = pitch,
			Distance = distance,
		};
	}

	private static float ComputeDistance(
		float boundsHalfX,
		float boundsHalfZ,
		float viewportWidth,
		float viewportHeight,
		float pitchRadians)
	{
		var mapSpan = System.Math.Max(boundsHalfX, boundsHalfZ) * 2f;
		if (mapSpan <= 0.001f)
			return Limits.MinDistance;

		var aspect = viewportWidth > 0f && viewportHeight > 0f
			? viewportWidth / viewportHeight
			: 16f / 9f;
		var verticalHalfFov = VerticalFovDegrees * MathF.PI / 180f * 0.5f;
		var horizontalHalfFov = MathF.Atan(MathF.Tan(verticalHalfFov) * aspect);
		var sinPitch = System.Math.Max(MathF.Sin(pitchRadians), 0.15f);
		var cosPitch = System.Math.Max(MathF.Cos(pitchRadians), 0.15f);

		var distanceForWidth = mapSpan / (2f * MathF.Tan(horizontalHalfFov) * cosPitch);
		var distanceForHeight = mapSpan / (2f * MathF.Tan(verticalHalfFov) * sinPitch);
		return System.Math.Max(distanceForWidth, distanceForHeight) * FramingMargin;
	}
}
