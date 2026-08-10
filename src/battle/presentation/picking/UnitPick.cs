using Godot;
using GrimSpace.Battle.Presentation.Graphics;

namespace GrimSpace.Battle.Presentation.Picking;

public static class UnitPick
{
	private const float PickRadiusPx = 35f;

	public static string? Pick(
		Camera3D camera,
		Vector2 screenPos,
		IReadOnlyDictionary<string, UnitView> unitViews)
	{
		string? bestId = null;
		var bestScreenDist = PickRadiusPx;
		var bestCameraDist = float.MaxValue;

		foreach (var (id, view) in unitViews)
		{
			if (!view.Visible)
				continue;

			var worldPos = view.GlobalPosition;
			if (camera.IsPositionBehind(worldPos))
				continue;

			var screenDist = camera.UnprojectPosition(worldPos).DistanceTo(screenPos);
			if (screenDist > PickRadiusPx)
				continue;

			var cameraDist = camera.GlobalPosition.DistanceTo(worldPos);
			if (!IsBetter(screenDist, cameraDist, bestScreenDist, bestCameraDist))
				continue;

			bestId = id;
			bestScreenDist = screenDist;
			bestCameraDist = cameraDist;
		}

		return bestId;
	}

	private static bool IsBetter(
		float screenDist,
		float cameraDist,
		float bestScreenDist,
		float bestCameraDist)
	{
		if (screenDist < bestScreenDist)
			return true;

		if (screenDist > bestScreenDist)
			return false;

		return cameraDist < bestCameraDist;
	}
}
