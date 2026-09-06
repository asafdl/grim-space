using Godot;

namespace GrimSpace.Presentation.Ui.Hud;

public static class HudStyles
{
	public const int Margin = 20;
	public const int HalfMargin = 10;
	public const int BodyScrollMinHeight = 192;

	public static void ApplyTextRole(Label label, HudTextRole role) =>
		label.ThemeTypeVariation = TextVariation(role);

	public static Color TextColor(HudTextRole role) =>
		ThemeDB.GetProjectTheme().GetColor("font_color", $"{TextVariation(role)}Label");

	public static void StyleButton(Button button, HudActionKind kind) =>
		button.ThemeTypeVariation = kind switch
		{
			HudActionKind.Primary => "PrimaryButton",
			HudActionKind.Destructive => "DestructiveButton",
			_ => "SecondaryButton",
		};

	public static void SetPanelVariation(PanelContainer panel, string variation) =>
		panel.ThemeTypeVariation = variation;

	public static string StatusPanelVariation(HudStatusKind kind) =>
		kind switch
		{
			HudStatusKind.Success => "StatusSuccess",
			HudStatusKind.Warning => "StatusWarning",
			HudStatusKind.Error => "StatusError",
			_ => "StatusNeutral",
		};

	public static string TextVariation(HudTextRole role) =>
		role switch
		{
			HudTextRole.Metadata => "Metadata",
			HudTextRole.Emphasis => "Emphasis",
			HudTextRole.Success => "Success",
			HudTextRole.Warning => "Warning",
			HudTextRole.Danger => "Danger",
			_ => "Body",
		};
}
