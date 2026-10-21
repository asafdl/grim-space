using Godot;

namespace GrimSpace.Presentation.Dev.Hud;

internal static class UiHudPreviewChrome
{
	public static Label Attach(Control owner, string title, string hints)
	{
		owner.AddChild(new ColorRect
		{
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Color = new Color(0.005f, 0.012f, 0.025f),
		});

		var label = new Label
		{
			OffsetLeft = 16,
			OffsetTop = 12,
			OffsetRight = 520,
			OffsetBottom = 80,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Text = $"{title}\n{hints}",
		};
		label.AddThemeColorOverride("font_color", new Color(0.55f, 0.62f, 0.72f));
		label.AddThemeFontSizeOverride("font_size", 14);
		owner.AddChild(label);
		return label;
	}

	public static void MountOverlay(Control owner, Control overlay)
	{
		overlay.AnchorsPreset = (int)Control.LayoutPreset.FullRect;
		overlay.AnchorRight = 1f;
		overlay.AnchorBottom = 1f;
		overlay.GrowHorizontal = Control.GrowDirection.Both;
		overlay.GrowVertical = Control.GrowDirection.Both;
		owner.AddChild(overlay);
	}
}
