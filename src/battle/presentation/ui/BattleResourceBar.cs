using Godot;
using GrimSpace.Components;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class BattleResourceBar : Control
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
	private const float FillBorderPadding = 4f;
	private const int RailDrawSliceCount = 32;
	private const float IconCenterX = 0.17f;
	private const float IconSize = 22f;
	private const float ValueStartX = 0.335f;
	private const float ValueWidth = 72f;
	private const float ValueHeight = 28f;
	private const float BarWidth = 240f;
	private const float BarHeight = 42f;

	private readonly Color _resourceColor;
	private BattleHudTooltipContent _tooltipContent;
	private readonly TextureRect _rail;
	private readonly TextureRect _icon;
	private readonly Label _valueLabel;
	private int _current;
	private int _maximum;

	public BattleResourceBar(
		string? name,
		Color color,
		BattleHudTooltipContent tooltip,
		string? iconPath = null,
		Color? iconTint = null)
	{
		if (iconPath is null && name is null)
			throw new ArgumentException("A resource bar requires either a name or an icon.", nameof(name));

		_resourceColor = color;
		_tooltipContent = tooltip;
		CustomMinimumSize = new Vector2(BarWidth, BarHeight);
		MouseFilter = MouseFilterEnum.Stop;
		TooltipText = tooltip.TriggerText;

		_rail = new TextureRect
		{
			Texture = GD.Load<Texture2D>(RailTexturePath),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale,
			Modulate = color.Lightened(0.24f),
			MouseFilter = MouseFilterEnum.Ignore,
		};
		AddChild(_rail);

		if (iconPath is not null)
		{
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
		}
		else
		{
			_icon = new TextureRect { Visible = false };
		}

		_valueLabel = new Label
		{
			MouseFilter = MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Center,
			ClipText = true,
			Text = "0/0",
		};
		_valueLabel.AddThemeFontSizeOverride("font_size", 16);
		_valueLabel.AddThemeColorOverride("font_color", Colors.White);
		_valueLabel.AddThemeColorOverride("font_outline_color", new Color(0.025f, 0.025f, 0.045f, 0.96f));
		_valueLabel.AddThemeConstantOverride("outline_size", 3);
		AddChild(_valueLabel);

		Resized += LayoutOverlays;
		LayoutOverlays();
	}

	public void SetTooltip(BattleHudTooltipContent tooltip)
	{
		_tooltipContent = tooltip;
		TooltipText = tooltip.TriggerText;
	}

	public void Apply(int current, int max)
	{
		_current = current;
		_maximum = max;
		_valueLabel.Text = $"{current}/{max}";
		QueueRedraw();
	}

	public override Control _MakeCustomTooltip(string forText) =>
		BattleHudTooltipCard.Create(_tooltipContent);

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

	public override void _Draw()
	{
		if (Size.X <= 0f || Size.Y <= 0f)
			return;

		var railOffsetY = (Size.Y - RailArtworkHeight) * 0.5f;
		var scaleX = Size.X / RailSourceWidth;
		var scaleY = RailArtworkHeight / RailSourceHeight;
		var top = railOffsetY + RailTopSourceY * scaleY;
		var bottom = railOffsetY + RailBottomSourceY * scaleY;
		var center = (top + bottom) * 0.5f;
		var startTop = RailLeftTopSourceX * scaleX - RailBorderOverlap;
		var startCenter = RailLeftCenterSourceX * scaleX - RailBorderOverlap;
		var startBottom = RailLeftBottomSourceX * scaleX - RailBorderOverlap;
		var end = Size.X * RailRight;
		var slash = Size.X * RailEndSlash;

		DrawRailBand(startTop, startCenter, startBottom, end, top, bottom, slash, TrackColor());

		if (_maximum <= 0 || _current <= 0)
			return;

		var fillTop = top + FillBorderPadding;
		var fillBottom = bottom - FillBorderPadding;
		var fillStartTop = startTop;
		var fillStartCenter = startCenter;
		var fillStartBottom = startBottom;
		var fillEnd = end - FillBorderPadding;
		var fraction = Mathf.Clamp(_current / (float)_maximum, 0f, 1f);
		var filledEnd = Mathf.Lerp(fillStartCenter, fillEnd, fraction);
		var filledSlash = Mathf.Min(slash, Mathf.Max(1f, (filledEnd - fillStartTop) * 0.45f));
		var glowEnd = filledEnd - filledSlash * 0.5f;
		DrawRailBand(
			fillStartTop,
			fillStartCenter,
			fillStartBottom,
			filledEnd,
			fillTop,
			fillBottom,
			filledSlash,
			_resourceColor,
			gradient: true);

		var fillCenter = (fillTop + fillBottom) * 0.5f;
		var glowStart = fillStartCenter + 4f;
		foreach (var (width, alpha, tint) in new[]
		{
			(12f, 0.18f, 0.26f),
			(8f, 0.28f, 0.42f),
			(4f, 0.42f, 0.6f),
			(2f, 0.62f, 0.82f),
		})
		{
			DrawLine(
				new Vector2(glowStart, fillCenter),
				new Vector2(glowEnd, fillCenter),
				WithAlpha(_resourceColor.Lightened(tint), alpha),
				width,
				true);
		}

		var fillEdge = new List<Vector2>(RailDrawSliceCount + 1);
		for (var i = 0; i <= RailDrawSliceCount; i++)
		{
			var t = i / (float)RailDrawSliceCount;
			fillEdge.Add(new Vector2(
				RailLeftAt(fillStartTop, fillStartCenter, fillStartBottom, t) + 1.25f,
				Mathf.Lerp(fillTop, fillBottom, t)));
		}
		DrawPolyline(
			fillEdge.ToArray(),
			WithAlpha(_resourceColor.Lightened(0.66f), 0.2f),
			5f,
			true);
		DrawPolyline(
			fillEdge.ToArray(),
			WithAlpha(_resourceColor.Lightened(0.82f), 0.9f),
			1.25f,
			true);

		var highlightStart = RailLeftAt(fillStartTop, fillStartCenter, fillStartBottom, 0.18f);
		DrawLine(
			new Vector2(highlightStart + 1f, Mathf.Lerp(fillTop, fillBottom, 0.18f)),
			new Vector2(glowEnd, Mathf.Lerp(fillTop, fillBottom, 0.18f)),
			WithAlpha(_resourceColor.Lightened(0.72f), 0.4f),
			1f,
			true);

	}

	private static Color WithAlpha(Color color, float alpha) =>
		new(color.R, color.G, color.B, alpha);

	private Color TrackColor()
	{
		var dark = _resourceColor.Darkened(0.78f);
		return new Color(dark.R, dark.G, dark.B, 0.88f);
	}

	private void DrawRailBand(
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
				var gradient = color.Darkened(0.3f).Lerp(color.Lightened(0.38f), progress);
				return gradient.Lightened(0.6f * crossGlow);
			}

			DrawPolygon(points, points.Select(AtPosition).ToArray());
		}
	}

	private static float RailLeftAt(float startTop, float startCenter, float startBottom, float t)
	{
		var averageStart = (startTop + startBottom) * 0.5f;
		return Mathf.Lerp(startTop, startBottom, t)
			+ Mathf.Sin(t * Mathf.Pi) * (startCenter - averageStart);
	}

}
