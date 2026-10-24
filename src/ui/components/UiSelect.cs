using Godot;
using GrimSpace.Components;

namespace GrimSpace.Ui.Components;

public sealed partial class UiSelect : OptionButton, IUiSized
{
	private readonly UiSizing _sizing;
	public UiSize DefaultSize => _sizing.DefaultSize;
	public UiSizeOverride? SizeOverride
	{
		get => _sizing.SizeOverride;
		set => _sizing.SizeOverride = value;
	}

	private HudPalette _palette;

	public UiSelect(HudPalette? palette = null)
	{
		_palette = palette ?? HudPalettes.BlueCyan;
		_sizing = new UiSizing(this, new UiSize(220f, 38f));
		FocusMode = FocusModeEnum.All;
		MouseDefaultCursorShape = CursorShape.PointingHand;
		AddThemeFontSizeOverride("font_size", 14);
		SetPalette(_palette);
	}

	public void SetPalette(HudPalette palette)
	{
		_palette = palette;
		AddThemeStyleboxOverride("normal", CreateFieldStyle(_palette.DeepSurface, _palette.Secondary));
		AddThemeStyleboxOverride(
			"hover",
			CreateFieldStyle(_palette.DeepSurface.Lerp(_palette.Highlight, 0.12f), _palette.Highlight, 2));
		AddThemeStyleboxOverride(
			"pressed",
			CreateFieldStyle(_palette.DeepSurface.Lerp(_palette.Highlight, 0.2f), _palette.Highlight, 2));
		AddThemeStyleboxOverride(
			"focus",
			CreateFieldStyle(_palette.DeepSurface, _palette.Highlight, 2));
		AddThemeStyleboxOverride(
			"disabled",
			CreateFieldStyle(_palette.DeepSurface, HudPalettes.WithAlpha(_palette.Secondary, 0.35f)));

		AddThemeColorOverride("font_color", _palette.Primary);
		AddThemeColorOverride("font_hover_color", Colors.White);
		AddThemeColorOverride("font_pressed_color", Colors.White);
		AddThemeColorOverride("font_disabled_color", HudPalettes.WithAlpha(_palette.Primary, 0.45f));

		var popup = GetPopup();
		popup.AddThemeStyleboxOverride("panel", CreatePopupPanelStyle());
		popup.AddThemeStyleboxOverride("hover", CreatePopupHoverStyle());
		popup.AddThemeColorOverride("font_color", _palette.Primary);
		popup.AddThemeColorOverride("font_hover_color", Colors.White);
		popup.AddThemeColorOverride("font_disabled_color", HudPalettes.WithAlpha(_palette.Secondary, 0.5f));
		popup.AddThemeColorOverride("font_accelerator_color", HudPalettes.WithAlpha(_palette.Secondary, 0.75f));
	}

	private StyleBoxFlat CreatePopupPanelStyle()
	{
		var style = new StyleBoxFlat
		{
			BgColor = _palette.DeepSurface,
			BorderColor = HudPalettes.WithAlpha(_palette.Secondary, 0.8f),
			ContentMarginLeft = 4f,
			ContentMarginTop = 4f,
			ContentMarginRight = 4f,
			ContentMarginBottom = 4f,
		};
		style.SetBorderWidthAll(1);
		return style;
	}

	private StyleBoxFlat CreatePopupHoverStyle()
	{
		var style = new StyleBoxFlat
		{
			BgColor = _palette.DeepSurface.Lerp(_palette.Highlight, 0.24f),
		};
		style.SetBorderWidthAll(0);
		return style;
	}

	private static StyleBoxFlat CreateFieldStyle(Color surface, Color edge, int borderWidth = 1)
	{
		var style = new StyleBoxFlat
		{
			BgColor = surface,
			BorderColor = edge,
			ContentMarginLeft = 12f,
			ContentMarginTop = 6f,
			ContentMarginRight = 30f,
			ContentMarginBottom = 6f,
		};
		style.SetBorderWidthAll(borderWidth);
		return style;
	}
}
