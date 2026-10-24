using Godot;
using GrimSpace.Components;

namespace GrimSpace.Ui.Components;

public sealed partial class UiKeycap : Button, IUiSized
{
	private readonly UiSizing _sizing;
	public UiSize DefaultSize => _sizing.DefaultSize;
	public UiSizeOverride? SizeOverride
	{
		get => _sizing.SizeOverride;
		set => _sizing.SizeOverride = value;
	}

	private HudPalette _palette;

	public UiKeycap(string text, HudPalette? palette = null)
	{
		Text = text;
		_palette = palette ?? HudPalettes.BlueCyan;
		_sizing = new UiSizing(this, new UiSize(64f, 34f));
		FocusMode = FocusModeEnum.All;
		MouseDefaultCursorShape = CursorShape.PointingHand;
		Alignment = HorizontalAlignment.Center;
		AddThemeFontSizeOverride("font_size", 13);
		SetPalette(_palette);
	}

	public void SetPalette(HudPalette palette)
	{
		_palette = palette;
		AddThemeStyleboxOverride("normal", CreateKeyStyle(_palette.DeepSurface, _palette.Secondary));
		AddThemeStyleboxOverride(
			"hover",
			CreateKeyStyle(_palette.DeepSurface.Lerp(_palette.Highlight, 0.14f), _palette.Highlight, 2));
		AddThemeStyleboxOverride(
			"pressed",
			CreateKeyStyle(_palette.DeepSurface.Lerp(_palette.Highlight, 0.24f), _palette.Highlight, 2));
		AddThemeStyleboxOverride(
			"focus",
			CreateKeyStyle(_palette.DeepSurface, _palette.Highlight, 2));
		AddThemeStyleboxOverride(
			"disabled",
			CreateKeyStyle(_palette.DeepSurface, HudPalettes.WithAlpha(_palette.Secondary, 0.35f)));
		AddThemeColorOverride("font_color", _palette.Primary);
		AddThemeColorOverride("font_hover_color", Colors.White);
		AddThemeColorOverride("font_pressed_color", Colors.White);
		AddThemeColorOverride("font_disabled_color", HudPalettes.WithAlpha(_palette.Primary, 0.45f));
	}

	private static StyleBoxFlat CreateKeyStyle(Color surface, Color edge, int borderWidth = 1)
	{
		var style = new StyleBoxFlat
		{
			BgColor = surface,
			BorderColor = edge,
			ContentMarginLeft = 8f,
			ContentMarginTop = 4f,
			ContentMarginRight = 8f,
			ContentMarginBottom = 4f,
		};
		style.SetBorderWidthAll(borderWidth);
		return style;
	}
}
