using Godot;
using GrimSpace.Battle.Presentation.Ui;

namespace GrimSpace.Presentation.Ui.Hud;

public static class HudStyles
{
	public const string ThemePath = "res://assets/ui/themes/hud_theme.tres";
	private const string FontRoleMeta = "hud_font_role";
	private const float MinHudScale = 1.05f;
	private const float Vt323Boost = 1.2f;

	private static Theme? _theme;

	public static Theme Theme => _theme ??= GD.Load<Theme>(ThemePath);

	public static void ApplyTheme(Control root) => root.Theme = Theme;

	public static void ApplyFont(Control node, HudFontRole role, Viewport? viewport = null, bool setVariation = true)
	{
		if (setVariation && node is Label)
			node.ThemeTypeVariation = LabelVariation(role);

		node.AddThemeFontSizeOverride("font_size", FontSize(role, viewport));
		node.SetMeta(FontRoleMeta, (int)role);
	}

	public static void ApplyTextRole(Label label, HudTextRole role, Viewport? viewport = null)
	{
		label.ThemeTypeVariation = TextVariation(role);
		ApplyFont(label, FontRoleForText(role), viewport, setVariation: false);
	}

	public static void RefreshFonts(Node root, Viewport viewport)
	{
		if (root is Control control && control.HasMeta(FontRoleMeta))
			ApplyFont(control, (HudFontRole)(int)control.GetMeta(FontRoleMeta), viewport);

		foreach (var child in root.GetChildren())
			RefreshFonts(child, viewport);
	}

	public static float ScaleFactor(Viewport? viewport = null) =>
		Mathf.Max(UiScale.Factor(viewport), MinHudScale) * Vt323Boost;

	public static Color TextColor(HudTextRole role) =>
		Theme.GetColor("font_color", $"{TextVariation(role)}Label");

	public static HudFontRole FontRoleForText(HudTextRole role) =>
		role switch
		{
			HudTextRole.Metadata => HudFontRole.Metadata,
			HudTextRole.Emphasis => HudFontRole.Emphasis,
			HudTextRole.Success => HudFontRole.Body,
			HudTextRole.Warning => HudFontRole.Body,
			HudTextRole.Danger => HudFontRole.Body,
			_ => HudFontRole.Body,
		};

	public static void StyleButton(Button button, HudActionKind kind, Viewport? viewport = null)
	{
		button.ThemeTypeVariation = kind switch
		{
			HudActionKind.Primary => "Primary",
			HudActionKind.Destructive => "Destructive",
			_ => "Secondary",
		};
		ApplyFont(button, HudFontRole.Button, viewport, setVariation: false);
		button.CustomMinimumSize = new Vector2(0, UiScale.Px(48f, viewport) * Vt323Boost);
	}

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

	public static int Margin(Viewport? viewport = null) =>
		Mathf.RoundToInt(20 * ScaleFactor(viewport));

	public static int FontSize(HudFontRole role, Viewport? viewport = null)
	{
		var design = role switch
		{
			HudFontRole.Title => 40,
			HudFontRole.Subtitle => 22,
			HudFontRole.CardTitle => 32,
			HudFontRole.SectionHeading => 18,
			HudFontRole.Body => 22,
			HudFontRole.Metadata => 20,
			HudFontRole.Emphasis => 28,
			HudFontRole.Button => 20,
			HudFontRole.Status => 24,
			HudFontRole.HeaderControl => 26,
			_ => 22,
		};
		return Mathf.RoundToInt(design * ScaleFactor(viewport));
	}

	private static string LabelVariation(HudFontRole role) =>
		role switch
		{
			HudFontRole.Title => "Title",
			HudFontRole.Subtitle => "Subtitle",
			HudFontRole.CardTitle => "CardTitle",
			HudFontRole.SectionHeading => "SectionHeading",
			HudFontRole.Metadata => "Metadata",
			HudFontRole.Emphasis => "Emphasis",
			HudFontRole.Status => "Status",
			HudFontRole.HeaderControl => "Body",
			HudFontRole.Button => "Body",
			_ => "Body",
		};

	private static string TextVariation(HudTextRole role) =>
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
