using Godot;
using GrimSpace.Components;

namespace GrimSpace.Ui.Components;

public sealed partial class UiFramedPortrait : Control, IUiSized
{
	private readonly UiSizing _sizing;
	public UiSize DefaultSize => _sizing.DefaultSize;
	public UiSizeOverride? SizeOverride
	{
		get => _sizing.SizeOverride;
		set => _sizing.SizeOverride = value;
	}

	private const string CircleClipShaderPath =
		"res://assets/ui/components/runtime/circle_clip.gdshader";
	private const string FrameTexturePath =
		"res://assets/ui/components/runtime/list_item_image_frame_base.png";
	private const string PaletteShaderPath =
		"res://assets/ui/components/runtime/button_palette.gdshader";
	private const float ImageDiameterRatio = 0.66f;

	private readonly TextureRect _frame;
	private readonly TextureRect _image;
	private readonly ShaderMaterial? _ownedPaletteMaterial;

	public UiFramedPortrait(
		Texture2D portrait,
		ShaderMaterial? framePaletteMaterial = null,
		HudPalette? palette = null)
	{
		ArgumentNullException.ThrowIfNull(portrait);
		_sizing = new UiSizing(this, new UiSize(0f, 0f));
		MouseFilter = MouseFilterEnum.Ignore;

		ShaderMaterial? material = framePaletteMaterial;
		if (material is null)
		{
			var shader = GD.Load<Shader>(PaletteShaderPath)
				?? throw new InvalidOperationException(
					$"Could not load framed portrait palette shader '{PaletteShaderPath}'.");
			_ownedPaletteMaterial = new ShaderMaterial { Shader = shader };
			ApplyPalette(palette ?? HudPalettes.BlueCyan);
			material = _ownedPaletteMaterial;
		}

		_frame = new TextureRect
		{
			Texture = LoadTexture(FrameTexturePath),
			Material = material,
			MouseFilter = MouseFilterEnum.Ignore,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale,
		};
		AddChild(_frame);

		var circleClip = GD.Load<Shader>(CircleClipShaderPath)
			?? throw new InvalidOperationException(
				$"Could not load circle clip shader '{CircleClipShaderPath}'.");
		_image = new TextureRect
		{
			Texture = portrait,
			Material = new ShaderMaterial { Shader = circleClip },
			MouseFilter = MouseFilterEnum.Ignore,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
		};
		AddChild(_image);

		Resized += LayoutPortrait;
		Callable.From(LayoutPortrait).CallDeferred();
	}

	public void SetPortrait(Texture2D portrait)
	{
		ArgumentNullException.ThrowIfNull(portrait);
		_image.Texture = portrait;
	}

	public void SetFramePaletteMaterial(ShaderMaterial material) => _frame.Material = material;

	public void ApplyPalette(HudPalette palette)
	{
		if (_ownedPaletteMaterial is null)
			return;

		_ownedPaletteMaterial.SetShaderParameter("surface_color", palette.DeepSurface);
		_ownedPaletteMaterial.SetShaderParameter("edge_color", palette.Secondary);
		_ownedPaletteMaterial.SetShaderParameter("palette_mix", 1f);
		_ownedPaletteMaterial.SetShaderParameter("brightness", 1f);
	}

	private void LayoutPortrait()
	{
		if (Size.X <= 0f || Size.Y <= 0f)
			return;

		var radius = Mathf.Min(Size.X, Size.Y) * 0.5f;
		var center = Size * 0.5f;
		_frame.Position = center - Vector2.One * radius;
		_frame.Size = Vector2.One * radius * 2f;

		var imageRadius = radius * ImageDiameterRatio;
		_image.Position = center - Vector2.One * imageRadius;
		_image.Size = Vector2.One * imageRadius * 2f;
	}

	private static Texture2D LoadTexture(string path) =>
		GD.Load<Texture2D>(path)
		?? throw new InvalidOperationException($"Could not load framed portrait texture '{path}'.");
}
