using Godot;
using GrimSpace.Components;

namespace GrimSpace.Ui.Components;

public sealed partial class UiImageListItem : UiListItem
{
	private const string BracketTexturePath =
		"res://assets/ui/components/runtime/list_item_bracket_base.png";
	private const float DefaultRowHeight = 110f;
	private const float BracketArtHeight = 300f;
	private const float BracketCircleCenterY = 136f;
	private const float BracketCircleDiameter = 245f;
	private const float PortraitDiameter = DefaultRowHeight * BracketCircleDiameter / BracketArtHeight;
	private const float PortraitGap = 0.2f;
	private const float PortraitOverlap = 18f;

	private readonly TextureRect _bracket;
	private readonly UiFramedPortrait _portrait;

	public UiImageListItem(
		string title,
		string metadata,
		string trailingValue,
		Texture2D image,
		HudPalette? palette = null)
		: base(title, metadata, trailingValue, palette)
	{
		ArgumentNullException.ThrowIfNull(image);

		Sizing.SetDefaultSize(new UiSize(0f, DefaultRowHeight));
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SetContentMargins(60, Mathf.CeilToInt(PortraitDiameter + PortraitGap + 48f), 12, 24);
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

		_portrait = new UiFramedPortrait(image, PaletteMaterial);
		AddChild(_portrait);

		Resized += LayoutImage;
		Callable.From(LayoutImage).CallDeferred();
	}

	public void SetImage(Texture2D image) => _portrait.SetPortrait(image);

	private void LayoutImage()
	{
		if (Size.X <= 0f || Size.Y <= 0f)
			return;

		var bodyWidth = Mathf.Max(
			0f,
			Size.X - PortraitGap - PortraitDiameter + PortraitOverlap);
		_bracket.Position = Vector2.Zero;
		_bracket.Size = new Vector2(bodyWidth, Size.Y);

		var portraitLeft = bodyWidth + PortraitGap - PortraitOverlap;
		var radius = PortraitDiameter * 0.5f;
		var centerY = (Size.Y - DefaultRowHeight) * 0.5f
			+ DefaultRowHeight * BracketCircleCenterY / BracketArtHeight;
		_portrait.Position = new Vector2(portraitLeft, centerY - radius);
		_portrait.Size = Vector2.One * PortraitDiameter;
	}

	private static Texture2D LoadTexture(string path) =>
		GD.Load<Texture2D>(path)
		?? throw new InvalidOperationException($"Could not load image-list frame texture '{path}'.");
}
