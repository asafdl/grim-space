using Godot;

namespace GrimSpace.Battle.Presentation.Ui;

/// <summary>Scales HUD metrics from a 1920×1080 design baseline.</summary>
internal static class UiScale
{
	public const float DesignWidth = 1920f;
	public const float DesignHeight = 1080f;

	public static float Factor(Viewport? viewport = null)
	{
		var size = ViewportSize(viewport);
		var factor = size.Y / DesignHeight;
		return Mathf.Clamp(factor, 0.9f, 2.5f);
	}

	public static int Font(int designPixels, Viewport? viewport = null) =>
		Mathf.RoundToInt(designPixels * Factor(viewport));

	public static int Margin(int designPixels, Viewport? viewport = null) =>
		Mathf.RoundToInt(designPixels * Factor(viewport));

	public static float Px(float designPixels, Viewport? viewport = null) =>
		designPixels * Factor(viewport);

	private static Vector2 ViewportSize(Viewport? viewport)
	{
		if (viewport is not null)
			return viewport.GetVisibleRect().Size;

		var tree = Engine.GetMainLoop() as SceneTree;
		return tree?.Root.GetViewport().GetVisibleRect().Size
			?? new Vector2(DesignWidth, DesignHeight);
	}
}
