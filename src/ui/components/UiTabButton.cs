using Godot;
using GrimSpace.Components;

namespace GrimSpace.Ui.Components;

public sealed partial class UiTabButton : Button, IUiSized
{
	private readonly UiSizing _sizing;
	public UiSize DefaultSize => _sizing.DefaultSize;
	public UiSizeOverride? SizeOverride
	{
		get => _sizing.SizeOverride;
		set => _sizing.SizeOverride = value;
	}

	private HudPalette _palette;

	public UiTabButton(string text, HudPalette? palette = null)
	{
		Text = text;
		_palette = palette ?? HudPalettes.BlueCyan;
		ToggleMode = true;
		_sizing = new UiSizing(this, new UiSize(100f, 38f));
		FocusMode = FocusModeEnum.All;
		MouseDefaultCursorShape = CursorShape.PointingHand;
		SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		Toggled += _ => QueueRedraw();
		SetPalette(_palette);
	}

	public void SetPalette(HudPalette palette)
	{
		_palette = palette;
		var empty = new StyleBoxEmpty();
		AddThemeStyleboxOverride("normal", empty);
		AddThemeStyleboxOverride("hover", empty);
		AddThemeStyleboxOverride("pressed", empty);
		AddThemeStyleboxOverride("disabled", empty);
		AddThemeStyleboxOverride("focus", empty);
		AddThemeColorOverride("font_color", HudPalettes.WithAlpha(_palette.Secondary, 0.64f));
		AddThemeColorOverride("font_hover_color", _palette.Primary);
		AddThemeColorOverride("font_pressed_color", _palette.Highlight);
		AddThemeColorOverride("font_disabled_color", HudPalettes.WithAlpha(_palette.Secondary, 0.35f));
		AddThemeFontSizeOverride("font_size", 14);
		QueueRedraw();
	}

	public void SetSelected(bool selected) => ButtonPressed = selected;

	public override void _Draw()
	{
		if (!ButtonPressed)
			return;

		DrawRect(
			new Rect2(0f, Size.Y - 2f, Size.X, 2f),
			_palette.Highlight);
	}
}
