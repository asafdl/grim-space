using Godot;
using GrimSpace.Components;

namespace GrimSpace.Ui.Components;

public enum UiReticleVisual
{
	Blue,
	Cyan,
	Teal,
	White,
}

public sealed partial class UiReticle : TextureRect, IUiSized
{
	private readonly UiSizing _sizing;
	public UiSize DefaultSize => _sizing.DefaultSize;
	public UiSizeOverride? SizeOverride
	{
		get => _sizing.SizeOverride;
		set => _sizing.SizeOverride = value;
	}

	private const string TexturePath =
		"res://assets/ui/components/runtime/reticle_hollow_neutral.png";
	private const float TextureSize = 256f;

	public UiReticle(UiReticleVisual visual = UiReticleVisual.Blue)
	{
		Texture = GD.Load<Texture2D>(TexturePath)
			?? throw new InvalidOperationException(
				$"Could not load reticle texture '{TexturePath}'.");
		_sizing = new UiSizing(this, new UiSize(TextureSize, TextureSize));
		ExpandMode = ExpandModeEnum.IgnoreSize;
		StretchMode = StretchModeEnum.KeepAspectCentered;
		MouseFilter = MouseFilterEnum.Ignore;
		SetVisual(visual);
	}

	public void SetVisual(UiReticleVisual visual)
	{
		Modulate = visual switch
		{
			UiReticleVisual.Cyan => HudPalettes.BlueCyan.Secondary,
			UiReticleVisual.Teal => new Color(0.18f, 0.72f, 0.65f),
			UiReticleVisual.White => Colors.White,
			_ => HudPalettes.BlueCyan.Primary,
		};
	}
}
