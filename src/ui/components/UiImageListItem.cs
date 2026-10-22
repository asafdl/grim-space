using Godot;
using GrimSpace.Components;

namespace GrimSpace.Ui.Components;

public sealed partial class UiImageListItem : UiListItem
{
	private const string CircleClipShaderPath =
		"res://assets/ui/components/runtime/circle_clip.gdshader";
	private const string FrameTexturePath =
		"res://assets/ui/components/runtime/list_item_image_frame_base.png";
	private const string BracketTexturePath =
		"res://assets/ui/components/runtime/list_item_bracket_base.png";
	private const float BracketArtWidth = 1120f;
	private const float BracketArtHeight = 300f;
	private const float BracketTextLeftInsetX = 148f;
	private const float BracketCircleCenterY = 136f;
	private const float BracketCircleDiameter = 245f;
	private const float PortraitGap = 0.2f;
	private const float PortraitOverlap = 18f;
	private const float ImageDiameterRatio = 0.66f;

	private readonly TextureRect _bracket;
	private readonly TextureRect _frame;
	private readonly TextureRect _image;

	public UiImageListItem(
		string title,
		string metadata,
		string trailingValue,
		Texture2D image,
		HudPalette? palette = null)
		: base(title, metadata, trailingValue, palette)
	{
		ArgumentNullException.ThrowIfNull(image);

		HideBackground();
		_bracket = new TextureRect
		{
			Texture = LoadTexture(BracketTexturePath),
			Material = PaletteMaterial,
			MouseFilter = MouseFilterEnum.Ignore,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale,
		};
		AddBehindContent(_bracket);

		_frame = new TextureRect
		{
			Texture = LoadTexture(FrameTexturePath),
			Material = PaletteMaterial,
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
			Texture = image,
			Material = new ShaderMaterial { Shader = circleClip },
			MouseFilter = MouseFilterEnum.Ignore,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
		};
		AddChild(_image);

		Resized += LayoutImage;
		Callable.From(LayoutImage).CallDeferred();
	}

	public void SetImage(Texture2D image)
	{
		ArgumentNullException.ThrowIfNull(image);
		_image.Texture = image;
	}

	private void LayoutImage()
	{
		if (Size.X <= 0f || Size.Y <= 0f)
			return;

		var portraitDiameter = Size.Y * BracketCircleDiameter / BracketArtHeight;
		var bodyWidth = Mathf.Max(
			0f,
			Size.X - PortraitGap - portraitDiameter + PortraitOverlap);
		_bracket.Position = Vector2.Zero;
		_bracket.Size = new Vector2(bodyWidth, Size.Y);

		var portraitLeft = bodyWidth + PortraitGap - PortraitOverlap;
		var radius = portraitDiameter * 0.5f;
		var center = new Vector2(
			portraitLeft + radius,
			Size.Y * BracketCircleCenterY / BracketArtHeight);
		_frame.Position = center - Vector2.One * radius;
		_frame.Size = Vector2.One * radius * 2f;

		var imageRadius = radius * ImageDiameterRatio;
		_image.Position = center - Vector2.One * imageRadius;
		_image.Size = Vector2.One * imageRadius * 2f;
		var rightMargin = portraitDiameter + PortraitGap + 48f;
		var leftMargin = Mathf.CeilToInt(
			BracketTextLeftInsetX / BracketArtWidth * bodyWidth + 8f);
		SetContentMargins(leftMargin, Mathf.CeilToInt(rightMargin), 12, 12);
	}

	private static Texture2D LoadTexture(string path) =>
		GD.Load<Texture2D>(path)
		?? throw new InvalidOperationException($"Could not load image-list frame texture '{path}'.");
}
