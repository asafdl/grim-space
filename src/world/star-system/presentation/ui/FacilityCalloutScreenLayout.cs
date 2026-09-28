using Godot;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

/// <summary>
/// Keeps facility icon centers separated on screen so pins can angle when anchors stack.
/// </summary>
public static class FacilityCalloutScreenLayout
{
	public const float DefaultIconGap = 20f;
	private const int SeparationIterations = 16;

	public static Vector2[] SeparateIconCenters(
		ReadOnlySpan<Vector2> naturalCenters,
		Vector2 iconSize,
		float gap = DefaultIconGap)
	{
		if (naturalCenters.Length == 0)
			return [];

		var resolved = new Vector2[naturalCenters.Length];
		naturalCenters.CopyTo(resolved);

		for (var iter = 0; iter < SeparationIterations; iter++)
		{
			for (var i = 0; i < resolved.Length; i++)
			{
				for (var j = i + 1; j < resolved.Length; j++)
					SeparatePair(resolved, i, j, iconSize, gap);
			}
		}

		return resolved;
	}

	public static Vector2 ClampIconTopLeft(
		Vector2 iconCenter,
		Vector2 iconSize,
		float margin,
		float viewportWidth,
		float viewportHeight)
	{
		var topLeft = iconCenter - iconSize * 0.5f;
		topLeft.X = Mathf.Clamp(topLeft.X, margin, viewportWidth - iconSize.X - margin);
		topLeft.Y = Mathf.Clamp(topLeft.Y, margin, viewportHeight - iconSize.Y - margin);
		return topLeft;
	}

	private static void SeparatePair(
		Vector2[] centers,
		int i,
		int j,
		Vector2 iconSize,
		float gap)
	{
		var delta = centers[j] - centers[i];
		var overlapX = iconSize.X + gap - Mathf.Abs(delta.X);
		var overlapY = iconSize.Y + gap - Mathf.Abs(delta.Y);
		if (overlapX <= 0f || overlapY <= 0f)
			return;

		var parallelStack = Mathf.Abs(delta.X) < iconSize.X * 0.6f;
		if (parallelStack || overlapX <= overlapY)
		{
			var push = overlapX * 0.5f;
			var sign = delta.X >= 0f ? 1f : -1f;
			if (Mathf.Abs(delta.X) < 0.5f)
				sign = j > i ? 1f : -1f;

			centers[i].X -= push * sign;
			centers[j].X += push * sign;
			return;
		}

		var verticalPush = overlapY * 0.5f;
		var verticalSign = delta.Y >= 0f ? 1f : -1f;
		centers[i].Y -= verticalPush * verticalSign;
		centers[j].Y += verticalPush * verticalSign;
	}
}
