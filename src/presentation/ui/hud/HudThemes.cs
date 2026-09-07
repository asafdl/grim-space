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

	public static void Apply(Control root, HudThemeFamily family) =>
		root.Theme = Load(family);
}
