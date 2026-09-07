using Godot;

namespace GrimSpace.Presentation.Ui.Hud;

public static class HudStyles
{
	public const int Margin = 20;
	public const int HalfMargin = 10;
	public const int BodyScrollMinHeight = 192;
	public const string InformativeHudPanelType = "InformativeHudPanelContainer";
	public const string ObjectivesHudPanelType = "ObjectivesHudPanelContainer";
	public const string ObjectiveEntryPanelType = "ObjectiveEntryPanelContainer";
	public const string ObjectivesHeadingLabelType = "ObjectivesHeadingLabel";
	public const string ObjectiveIndexLabelType = "ObjectiveIndexLabel";
	public const string ObjectiveSeparatorLabelType = "ObjectiveSeparatorLabel";
	public const string ObjectiveTitleLabelType = "ObjectiveTitleLabel";
	public const string ObjectiveBodyLabelType = "ObjectiveBodyLabel";
	public const string HudHeadingLabelType = "HudHeadingLabel";
	public const string DebugHudPanelType = "DebugHudPanelContainer";
	public const int SectionAccentWidth = 3;
	public const int InformativeEntryGap = 14;
	public const int ObjectivesOuterPadding = 22;
	public const int ObjectivesHeaderBottomPadding = 14;
	public const int ObjectivesEntryGap = 12;
	public const int ObjectivesEntryInnerPadding = 12;
	public static readonly Color ModalBackdrop = new(0f, 0f, 0f, 0.55f);
	public static readonly Color AccentCyan = new(0.55f, 0.78f, 1f);
	public static readonly Color InformativeHairline = new(0.22f, 0.32f, 0.42f, 0.45f);

	public static string PanelVariation(HudThemeFamily family) =>
		family switch
		{
			HudThemeFamily.Debug => DebugHudPanelType,
			HudThemeFamily.Informative => InformativeHudPanelType,
			_ => "ShellPanelContainer",
		};

	public static string HudHeadingVariation(HudThemeFamily family) => HudHeadingLabelType;

	public static string EntryTitleVariation(HudThemeFamily family) =>
		family switch
		{
			HudThemeFamily.Debug => "DebugValueLabel",
			_ => "EntryTitleLabel",
		};

	public static string EntryBodyVariation(HudThemeFamily family) =>
		family switch
		{
			HudThemeFamily.Debug => "DebugValueLabel",
			_ => "EntryBodyLabel",
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
