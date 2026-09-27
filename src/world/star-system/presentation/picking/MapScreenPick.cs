using Godot;

namespace GrimSpace.World.StarSystem.Presentation.Picking;

public static class MapScreenPick
{
	public const float SnapMarginPixels = 12f;

	public static float DistancePixels(Camera3D camera, Vector3 worldPosition, Vector2 screenPos)
	{
		if (camera.IsPositionBehind(worldPosition))
			return float.MaxValue;

		return camera.UnprojectPosition(worldPosition).DistanceTo(screenPos);
	}

	public static bool WithinSnap(Camera3D camera, Vector3 worldPosition, Vector2 screenPos, out float distancePixels)
	{
		distancePixels = DistancePixels(camera, worldPosition, screenPos);
		return distancePixels <= SnapMarginPixels;
	}

	public static bool IsBetterHit(bool direct, float distance, bool bestDirect, float bestDistance)
	{
		if (direct != bestDirect)
			return direct;

		return distance < bestDistance;
	}
}
