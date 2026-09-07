using Godot;

namespace GrimSpace.Presentation.Ui.Hud;

public static class HudThemes
{
	public const string TheatricalPath = "res://assets/ui/themes/theatrical_theme.tres";
	public const string InformativePath = "res://assets/ui/themes/informative_theme.tres";
	public const string BattlePath = "res://assets/ui/themes/battle_hud_theme.tres";
	public const string DebugPath = "res://assets/ui/themes/debug_theme.tres";

	public static Theme Load(HudThemeFamily family) =>
		GD.Load<Theme>(family switch
		{
			HudThemeFamily.Theatrical => TheatricalPath,
			HudThemeFamily.Informative => InformativePath,
			HudThemeFamily.Battle => BattlePath,
			HudThemeFamily.Debug => DebugPath,
			_ => throw new ArgumentOutOfRangeException(nameof(family)),
		});

	public static Theme Apply(Control root, HudThemeFamily family)
	{
		var theme = Load(family);
		root.Theme = theme;
		return theme;
	}
}
