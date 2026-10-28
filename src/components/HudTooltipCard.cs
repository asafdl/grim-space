using Godot;

namespace GrimSpace.Components;

public sealed partial class HudTooltipCard : PanelContainer
{
	public static readonly StringName TooltipMetadataKey = "_map_tooltip_text";

	private const float CornerCut = 12f;
	private const float MaximumContentWidth = 360f;
	private const int FontSize = 18;
	private const string RegularFontPath = "res://assets/ui/fonts/Quantico-Regular.ttf";
	private static readonly Color FillColor = new(0.02f, 0.04f, 0.07f, 0.98f);
	private static readonly Color BorderColor = HudPalettes.WithAlpha(HudPalettes.BlueCyan.Highlight, 0.9f);
	private static readonly Color AccentColor = HudPalettes.BlueCyan.Secondary;
	private static readonly Font RegularFont = GD.Load<FontFile>(RegularFontPath);
	private readonly Label _label;

	public static void SetTooltip(Control control, string text)
	{
		ArgumentNullException.ThrowIfNull(control);
		control.SetMeta(TooltipMetadataKey, text);
		control.TooltipText = string.Empty;
	}

	public HudTooltipCard()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		Visible = false;
		AddThemeStyleboxOverride("panel", new StyleBoxEmpty
		{
			ContentMarginLeft = 8f,
			ContentMarginTop = 6f,
			ContentMarginRight = 8f,
			ContentMarginBottom = 6f,
		});

		_label = new Label
		{
			MouseFilter = MouseFilterEnum.Ignore,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			CustomMaximumSize = new Vector2(MaximumContentWidth, -1f),
		};
		_label.AddThemeFontOverride("font", RegularFont);
		_label.AddThemeFontSizeOverride("font_size", FontSize);
		_label.AddThemeColorOverride("font_color", HudPalettes.BlueCyan.Primary);
		AddChild(_label);
	}

	public void Present(string text, Vector2 screenPosition)
	{
		var availableContentWidth = Mathf.Min(
			MaximumContentWidth,
			Mathf.Max(1f, GetViewportRect().Size.X - 32f));
		var textWidth = text
			.Split('\n')
			.Select(line => RegularFont.GetStringSize(
				line,
				HorizontalAlignment.Left,
				-1,
				FontSize).X)
			.DefaultIfEmpty(0f)
			.Max();
		var contentWidth = Mathf.Clamp(textWidth, 1f, availableContentWidth);
		_label.Text = text;
		_label.CustomMinimumSize = new Vector2(contentWidth, 0f);
		_label.CustomMaximumSize = new Vector2(contentWidth, -1f);
		Visible = true;
		ResetSize();
		Size = GetCombinedMinimumSize();

		var viewportSize = GetViewportRect().Size;
		var desiredPosition = screenPosition + new Vector2(14f, 18f);
		Position = new Vector2(
			Mathf.Clamp(desiredPosition.X, 0f, Mathf.Max(0f, viewportSize.X - Size.X)),
			Mathf.Clamp(desiredPosition.Y, 0f, Mathf.Max(0f, viewportSize.Y - Size.Y)));
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (Size.X <= CornerCut || Size.Y <= CornerCut)
			return;

		var points = new[]
		{
			new Vector2(0f, 0f),
			new Vector2(Size.X - CornerCut, 0f),
			new Vector2(Size.X, CornerCut),
			new Vector2(Size.X, Size.Y),
			new Vector2(CornerCut, Size.Y),
			new Vector2(0f, Size.Y - CornerCut),
		};
		DrawPolygon(points, Enumerable.Repeat(FillColor, points.Length).ToArray());
		DrawPolyline(points.Append(points[0]).ToArray(), BorderColor, 1.5f, true);
		DrawLine(
			new Vector2(8f, 2f),
			new Vector2(Size.X - CornerCut - 2f, 2f),
			AccentColor,
			2f,
			true);
	}
}
