using Godot;
using GrimSpace.Components;

namespace GrimSpace.Ui.Components;

public sealed partial class UiSlider : HSlider
{
	private const string GrabberTexturePath =
		"res://assets/ui/components/runtime/slider_grabber_base.png";

	private readonly Texture2D _grabberTexture;
	private HudPalette _palette;

	public UiSlider(HudPalette? palette = null)
	{
		_palette = palette ?? HudPalettes.BlueCyan;
		MinValue = 0d;
		MaxValue = 100d;
		Step = 1d;
		CustomMinimumSize = new Vector2(220f, 28f);
		FocusMode = FocusModeEnum.All;
		MouseDefaultCursorShape = CursorShape.PointingHand;
		_grabberTexture = GD.Load<Texture2D>(GrabberTexturePath)
			?? throw new InvalidOperationException(
				$"Could not load slider grabber texture '{GrabberTexturePath}'.");

		AddThemeIconOverride("grabber", _grabberTexture);
		AddThemeIconOverride("grabber_highlight", _grabberTexture);
		AddThemeIconOverride("grabber_disabled", _grabberTexture);
		SetPalette(_palette);
	}

	public void SetPalette(HudPalette palette)
	{
		_palette = palette;
		AddThemeStyleboxOverride("slider", CreateTrackStyle());
		AddThemeStyleboxOverride("grabber_area", CreateActiveTrackStyle(false));
		AddThemeStyleboxOverride("grabber_area_highlight", CreateActiveTrackStyle(true));
	}

	public void SetEnabled(bool enabled)
	{
		Editable = enabled;
		Modulate = enabled
			? Colors.White
			: HudPalettes.WithAlpha(Colors.White, 0.45f);
	}

	private StyleBoxFlat CreateTrackStyle()
	{
		var style = new StyleBoxFlat
		{
			BgColor = _palette.DeepSurface,
			BorderColor = HudPalettes.WithAlpha(_palette.Secondary, 0.42f),
			ContentMarginTop = 8f,
			ContentMarginBottom = 8f,
		};
		style.SetBorderWidthAll(1);
		return style;
	}

	private StyleBoxFlat CreateActiveTrackStyle(bool highlighted)
	{
		var style = new StyleBoxFlat
		{
			BgColor = highlighted ? _palette.Highlight : _palette.Secondary,
			ContentMarginTop = 8f,
			ContentMarginBottom = 8f,
		};
		return style;
	}
}
