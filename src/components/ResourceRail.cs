using Godot;

namespace GrimSpace.Components;

/// <summary>
/// Reticle-and-rail HUD plate: icon inside the reticle, value text inside the rail.
/// Subclasses add rail fill by overriding <see cref="DrawFill"/>.
/// </summary>
public partial class ResourceRail : Control
{
	private const string RailTexturePath =
		"res://assets/ui/components/runtime/resource_rail_neutral_source.png";
	private const float RailArtworkHeight = 58f;
	private const float RailSourceWidth = 1359f;
	private const float RailSourceHeight = 388f;
	private const float RailCenterY = 0.532f;
	private const float RailTopSourceY = 151f;
	private const float RailBottomSourceY = 277f;
	private const float RailLeftTopSourceX = 384f;
	private const float RailLeftCenterSourceX = 414f;
	private const float RailLeftBottomSourceX = 367f;
	private const float RailRight = 0.998f;
	private const float RailEndSlash = 0.025f;
	private const float RailBorderOverlap = 1f;
	private const float IconCenterX = 0.17f;
	private const float IconSize = 22f;
	private const float ValueStartX = 0.335f;
	private const float ValueWidth = 72f;
	private const float ValueHeight = 28f;

	public const float BarWidth = 240f;
	public const float BarHeight = 42f;
	protected const int RailDrawSliceCount = 32;
	protected const float FillBorderPadding = 4f;

	/// <summary>Rail interior outline in local control coordinates.</summary>
	protected readonly record struct RailGeometry(
		float StartTop,
		float StartCenter,
		float StartBottom,
		float End,
		float Top,
		float Bottom,
		float Slash);

	private readonly Color _resourceColor;
	private readonly TextureRect _rail;
	private readonly TextureRect _icon;
	private readonly Label _valueLabel;

	public ResourceRail(Color color, string iconPath, Color? iconTint = null)
	{
		_resourceColor = color;
		CustomMinimumSize = new Vector2(BarWidth, BarHeight);
		MouseFilter = MouseFilterEnum.Ignore;

		_rail = new TextureRect
		{
			Texture = GD.Load<Texture2D>(RailTexturePath),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale,
			Modulate = color.Lightened(0.24f),
			MouseFilter = MouseFilterEnum.Ignore,
		};
		AddChild(_rail);

		_icon = new TextureRect
		{
			Texture = iconTint is { } tint
				? SvgIconLoader.LoadCentered(iconPath, tint, 32)
				: SvgIconLoader.LoadRawCentered(iconPath, 32),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		AddChild(_icon);

		_valueLabel = new Label
		{
			MouseFilter = MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Center,
			ClipText = true,
			Text = "0",
		};
		_valueLabel.AddThemeFontSizeOverride("font_size", 16);
		_valueLabel.AddThemeColorOverride("font_color", Colors.White);
		_valueLabel.AddThemeColorOverride("font_outline_color", new Color(0.025f, 0.025f, 0.045f, 0.96f));
		_valueLabel.AddThemeConstantOverride("outline_size", 3);
		AddChild(_valueLabel);

		Resized += LayoutOverlays;
		LayoutOverlays();
	}

	protected Color ResourceColor => _resourceColor;

	public void SetValueText(string text) => _valueLabel.Text = text;

	public override void _Draw()
	{
		if (Size.X <= 0f || Size.Y <= 0f)
			return;

		var geometry = CurrentGeometry();
		DrawRailBand(
			geometry.StartTop,
			geometry.StartCenter,
			geometry.StartBottom,
			geometry.End,
			geometry.Top,
			geometry.Bottom,
			geometry.Slash,
			TrackColor());
		DrawFill(geometry);
	}

	protected virtual void DrawFill(in RailGeometry geometry)
	{
	}

	protected RailGeometry CurrentGeometry()
	{
		var railOffsetY = (Size.Y - RailArtworkHeight) * 0.5f;
		var scaleX = Size.X / RailSourceWidth;
		var scaleY = RailArtworkHeight / RailSourceHeight;
		return new RailGeometry(
			StartTop: RailLeftTopSourceX * scaleX - RailBorderOverlap,
			StartCenter: RailLeftCenterSourceX * scaleX - RailBorderOverlap,
			StartBottom: RailLeftBottomSourceX * scaleX - RailBorderOverlap,
			End: Size.X * RailRight,
			Top: railOffsetY + RailTopSourceY * scaleY,
			Bottom: railOffsetY + RailBottomSourceY * scaleY,
			Slash: Size.X * RailEndSlash);
	}

	protected static Color WithAlpha(Color color, float alpha) =>
		new(color.R, color.G, color.B, alpha);

	protected static float RailLeftAt(float startTop, float startCenter, float startBottom, float t)
	{
		var averageStart = (startTop + startBottom) * 0.5f;
		return Mathf.Lerp(startTop, startBottom, t)
			+ Mathf.Sin(t * Mathf.Pi) * (startCenter - averageStart);
	}

	protected void DrawRailBand(
		float startTop,
		float startCenter,
		float startBottom,
		float end,
		float top,
		float bottom,
		float slash,
		Color color,
		bool gradient = false)
	{
		for (var i = 0; i < RailDrawSliceCount; i++)
		{
			var t0 = i / (float)RailDrawSliceCount;
			var t1 = (i + 1f) / RailDrawSliceCount;
			var overlap = gradient ? 0.35f : 0f;
			var y0 = Mathf.Lerp(top, bottom, t0) - overlap;
			var y1 = Mathf.Lerp(top, bottom, t1) + overlap;
			var left0 = RailLeftAt(startTop, startCenter, startBottom, t0);
			var left1 = RailLeftAt(startTop, startCenter, startBottom, t1);
			var right0 = end - slash * t0;
			var right1 = end - slash * t1;
			var points = new Vector2[]
			{
				new(left0, y0),
				new(right0, y0),
				new(right1, y1),
				new(left1, y1),
			};
			if (!gradient)
			{
				DrawColoredPolygon(points, color);
				continue;
			}

			Color AtPosition(Vector2 point)
			{
				var progress = Mathf.Clamp(
					(point.X - startCenter) / Mathf.Max(1f, end - startCenter),
					0f,
					1f);
				var crossGlow = 1f - Mathf.Abs(point.Y - (top + bottom) * 0.5f)
					/ Mathf.Max(1f, (bottom - top) * 0.5f);
				crossGlow = Mathf.Pow(Mathf.Clamp(crossGlow, 0f, 1f), 2f);
				var shaded = color.Darkened(0.3f).Lerp(color.Lightened(0.38f), progress);
				return shaded.Lightened(0.6f * crossGlow);
			}

			DrawPolygon(points, points.Select(AtPosition).ToArray());
		}
	}

	private Color TrackColor()
	{
		var dark = _resourceColor.Darkened(0.78f);
		return new Color(dark.R, dark.G, dark.B, 0.88f);
	}

	private void LayoutOverlays()
	{
		_rail.Position = new Vector2(0f, (Size.Y - RailArtworkHeight) * 0.5f);
		_rail.Size = new Vector2(Size.X, RailArtworkHeight);

		var iconCenterY = _rail.Position.Y + RailArtworkHeight * RailCenterY;
		var center = new Vector2(Size.X * IconCenterX, iconCenterY);
		_icon.Position = center - Vector2.One * (IconSize * 0.5f);
		_icon.Size = Vector2.One * IconSize;

		_valueLabel.Position = new Vector2(Size.X * ValueStartX, (Size.Y - ValueHeight) * 0.5f);
		_valueLabel.Size = new Vector2(ValueWidth, ValueHeight);
	}
}
