using Godot;
using GrimSpace.Application;
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

	public static Control ForBinding(KeyBinding binding)
	{
		var iconPath = binding.Device == BindingDevice.Mouse ? MouseIconPath(binding.MouseButton) : null;
		if (iconPath is not null)
		{
			return new TextureRect
			{
				Texture = GD.Load<Texture2D>(iconPath)
					?? throw new InvalidOperationException($"Could not load input glyph '{iconPath}'."),
				CustomMinimumSize = new Vector2(26f, 30f),
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				MouseFilter = MouseFilterEnum.Ignore,
			};
		}

		return new UiKeycap(GameInputBindings.LabelForBinding(binding))
		{
			SizeOverride = new UiSizeOverride(Width: 26f, Height: 26f),
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
			MouseFilter = MouseFilterEnum.Ignore,
			FocusMode = FocusModeEnum.None,
		};
	}

	public static string? MouseIconPath(MouseButton button) => button switch
	{
		MouseButton.Left => "res://assets/ui/input/mouse_left.svg",
		MouseButton.Right => "res://assets/ui/input/mouse_right.svg",
		MouseButton.Middle => "res://assets/ui/input/mouse_middle.svg",
		MouseButton.WheelUp or MouseButton.WheelDown => "res://assets/ui/input/mouse_scroll.svg",
		_ => null,
	};

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
