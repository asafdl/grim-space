using Godot;
using GrimSpace.Components;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class BattleResourceBar : ResourceRail
{
	private BattleHudTooltipContent _tooltipContent;
	private int _current;
	private int _maximum;

	public BattleResourceBar(
		Color color,
		BattleHudTooltipContent tooltip,
		string iconPath,
		Color? iconTint = null)
		: base(color, iconPath, iconTint)
	{
		_tooltipContent = tooltip;
		MouseFilter = MouseFilterEnum.Stop;
		TooltipText = tooltip.TriggerText;
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
		SetValueText($"{current}/{max}");
		QueueRedraw();
	}

	public override Control _MakeCustomTooltip(string forText) =>
		BattleHudTooltipCard.Create(_tooltipContent);

	protected override void DrawFill(in RailGeometry geometry)
	{
		if (_maximum <= 0 || _current <= 0)
			return;

		var (startTop, startCenter, startBottom, end, top, bottom, slash) = geometry;
		var fillTop = top + FillBorderPadding;
		var fillBottom = bottom - FillBorderPadding;
		var fillEnd = end - FillBorderPadding;
		var fraction = Mathf.Clamp(_current / (float)_maximum, 0f, 1f);
		var filledEnd = Mathf.Lerp(startCenter, fillEnd, fraction);
		var filledSlash = Mathf.Min(slash, Mathf.Max(1f, (filledEnd - startTop) * 0.45f));
		var glowEnd = filledEnd - filledSlash * 0.5f;
		DrawRailBand(
			startTop,
			startCenter,
			startBottom,
			filledEnd,
			fillTop,
			fillBottom,
			filledSlash,
			ResourceColor,
			gradient: true);

		var fillCenter = (fillTop + fillBottom) * 0.5f;
		var glowStart = startCenter + 4f;
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
				WithAlpha(ResourceColor.Lightened(tint), alpha),
				width,
				true);
		}

		var fillEdge = new List<Vector2>(RailDrawSliceCount + 1);
		for (var i = 0; i <= RailDrawSliceCount; i++)
		{
			var t = i / (float)RailDrawSliceCount;
			fillEdge.Add(new Vector2(
				RailLeftAt(startTop, startCenter, startBottom, t) + 1.25f,
				Mathf.Lerp(fillTop, fillBottom, t)));
		}
		DrawPolyline(
			fillEdge.ToArray(),
			WithAlpha(ResourceColor.Lightened(0.66f), 0.2f),
			5f,
			true);
		DrawPolyline(
			fillEdge.ToArray(),
			WithAlpha(ResourceColor.Lightened(0.82f), 0.9f),
			1.25f,
			true);

		var highlightStart = RailLeftAt(startTop, startCenter, startBottom, 0.18f);
		DrawLine(
			new Vector2(highlightStart + 1f, Mathf.Lerp(fillTop, fillBottom, 0.18f)),
			new Vector2(glowEnd, Mathf.Lerp(fillTop, fillBottom, 0.18f)),
			WithAlpha(ResourceColor.Lightened(0.72f), 0.4f),
			1f,
			true);
	}
}
