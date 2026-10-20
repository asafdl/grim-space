using Godot;
using GrimSpace.Components;

namespace GrimSpace.Presentation.Menu;

public static class MenuUi
{
	public static Theme Theme => HudThemes.Load(HudThemeFamily.MainMenu);

	public static void ApplyTheme(Control root) => root.Theme = Theme;

	public static Color ModalBackdropColor => HudStyles.ModalBackdrop;
}
