using Godot;

namespace GrimSpace.Presentation.Ui.Hud;

public static class HudStyles
{
	public const int Margin = 20;
	public const int HalfMargin = 10;
	public const int BodyScrollMinHeight = 192;
	public const string InformativePanelVariation = "InformativeHud";
	public const string DebugPanelVariation = "DebugHud";
	public static readonly Color ModalBackdrop = new(0f, 0f, 0f, 0.55f);

	public static string PanelVariation(HudThemeFamily family) =>
		family switch
		{
			HudThemeFamily.Debug => DebugPanelVariation,
			HudThemeFamily.Informative => InformativePanelVariation,
			_ => "Shell",
		};

	public static string HudHeadingVariation(HudThemeFamily family) => "HudHeading";

	public static string EntryTitleVariation(HudThemeFamily family) =>
		family switch
		{
			HudThemeFamily.Debug => "DebugValue",
			_ => "EntryTitle",
		};

	public static string EntryBodyVariation(HudThemeFamily family) =>
		family switch
		{
			HudThemeFamily.Debug => "DebugValue",
			_ => "EntryBody",
		};

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
