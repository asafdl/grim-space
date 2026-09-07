using Godot;

namespace GrimSpace.Presentation.Ui.Hud;

public static class HudThemes
{
	public const string TheatricalPath = "res://assets/ui/themes/theatrical_theme.tres";
	public const string InformativePath = "res://assets/ui/themes/informative_theme.tres";
	public const string DebugPath = "res://assets/ui/themes/debug_theme.tres";

	public static Theme Load(HudThemeFamily family) =>
		GD.Load<Theme>(family switch
		{
			HudThemeFamily.Theatrical => TheatricalPath,
			HudThemeFamily.Informative => InformativePath,
			HudThemeFamily.Debug => DebugPath,
			_ => throw new ArgumentOutOfRangeException(nameof(family)),
		});

	public static Theme Apply(Control root, HudThemeFamily family)
	{
		var theme = Load(family);
		root.Theme = theme;
		return theme;
	}

	public static void StylePanel(PanelContainer panel, Theme theme, string themeType)
	{
		panel.Theme = theme;
		panel.ThemeTypeVariation = themeType;
		var style = theme.GetStylebox("panel", themeType);
		if (style is not null)
			panel.AddThemeStyleboxOverride("panel", style);
	}

	public static void StyleLabel(Label label, Theme theme, string themeType)
	{
		label.Theme = theme;
		label.ThemeTypeVariation = themeType;
		label.AddThemeColorOverride("font_color", theme.GetColor("font_color", themeType));

		var fontSize = theme.GetFontSize("font_size", themeType);
		if (fontSize > 0)
			label.AddThemeFontSizeOverride("font_size", fontSize);

		var font = theme.GetFont("font", themeType);
		if (font is not null)
			label.AddThemeFontOverride("font", font);
	}
}
