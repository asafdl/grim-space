using Godot;

namespace GrimSpace.Components;

public sealed partial class HudTooltipCard : PanelContainer
{
	public static readonly StringName TooltipMetadataKey = "_map_tooltip_text";

	private const float CornerCut = 12f;
	private const float TooltipIconSize = 16f;
	private const float TooltipContentWidth = 360f;
	private const int BodyFontSize = 18;
	private const string RegularFontPath = "res://assets/ui/fonts/Quantico-Regular.ttf";
	private const string BoldFontPath = "res://assets/ui/fonts/Quantico-Bold.ttf";

	private static readonly Font RegularFont = GD.Load<FontFile>(RegularFontPath);
	private static readonly Font BoldFont = GD.Load<FontFile>(BoldFontPath);

	private readonly HudPalette _palette;
	private readonly Color _fillColor;
	private readonly Color _borderColor;
	private readonly Color _accentColor;
	private readonly Color _bodyColor;
	private readonly VBoxContainer _content;
	private string? _presentedText;

	public HudTooltipCard()
		: this(HudPalettes.BlueCyan)
	{
	}

	private HudTooltipCard(HudPalette palette)
	{
		_palette = palette;
		_fillColor = HudPalettes.WithAlpha(palette.Surface, 0.98f);
		_borderColor = HudPalettes.WithAlpha(palette.Highlight, 0.9f);
		_accentColor = palette.Secondary;
		_bodyColor = palette.Primary;
		MouseFilter = MouseFilterEnum.Ignore;
		Visible = false;
		AddThemeStyleboxOverride("panel", new StyleBoxEmpty
		{
			ContentMarginLeft = 8f,
			ContentMarginTop = 6f,
			ContentMarginRight = 8f,
			ContentMarginBottom = 6f,
		});

		_content = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		_content.AddThemeConstantOverride("separation", 6);
		AddChild(_content);
	}

	public static void SetTooltip(Control control, string text)
	{
		ArgumentNullException.ThrowIfNull(control);
		control.SetMeta(TooltipMetadataKey, text);
		control.TooltipText = string.Empty;
	}

	public static Control Create(HudTooltipContent tooltip, HudPalette palette)
	{
		ArgumentNullException.ThrowIfNull(tooltip);
		var card = new HudTooltipCard(palette);
		card.BuildContent(tooltip);
		card.Visible = true;
		card.ResetSize();
		card.CustomMinimumSize = card.GetCombinedMinimumSize();
		return card;
	}

	public void Present(string text, Vector2 screenPosition)
	{
		if (!string.Equals(_presentedText, text, StringComparison.Ordinal))
		{
			BuildContent(HudTooltipContent.Plain(text));
			_presentedText = text;
			CustomMinimumSize = Vector2.Zero;
			ResetSize();
			CustomMinimumSize = GetCombinedMinimumSize();
			Size = CustomMinimumSize;
			QueueRedraw();
		}

		Visible = true;

		var viewportSize = GetViewportRect().Size;
		var desiredPosition = screenPosition + new Vector2(14f, 18f);
		Position = new Vector2(
			Mathf.Clamp(desiredPosition.X, 0f, Mathf.Max(0f, viewportSize.X - Size.X)),
			Mathf.Clamp(desiredPosition.Y, 0f, Mathf.Max(0f, viewportSize.Y - Size.Y)));
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
		DrawPolygon(points, Enumerable.Repeat(_fillColor, points.Length).ToArray());
		DrawPolyline(points.Append(points[0]).ToArray(), _borderColor, 1.5f, true);
		DrawLine(
			new Vector2(8f, 2f),
			new Vector2(Size.X - CornerCut - 2f, 2f),
			_accentColor,
			2f,
			true);
	}

	private void BuildContent(HudTooltipContent tooltip)
	{
		foreach (var child in _content.GetChildren())
		{
			_content.RemoveChild(child);
			child.QueueFree();
		}

		if (!string.IsNullOrWhiteSpace(tooltip.Title))
			_content.AddChild(CreateTitle(tooltip.Title));
		if (!string.IsNullOrWhiteSpace(tooltip.Details))
			_content.AddChild(CreateFormattedText(tooltip.Details));
		if (!string.IsNullOrWhiteSpace(tooltip.Body))
			_content.AddChild(CreateFormattedText(tooltip.Body));
	}

	private RichTextLabel CreateTitle(string text)
	{
		var label = new RichTextLabel
		{
			MouseFilter = MouseFilterEnum.Ignore,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			FitContent = true,
			ScrollActive = false,
			CustomMaximumSize = new Vector2(TooltipContentWidth, -1f),
		};
		ApplyTextStyle(label, BoldFont, _palette.Title, rich: true);
		label.AddText(text);
		return label;
	}

	private RichTextLabel CreateFormattedText(string template)
	{
		var label = new RichTextLabel
		{
			MouseFilter = MouseFilterEnum.Ignore,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			FitContent = true,
			ScrollActive = false,
			CustomMaximumSize = new Vector2(TooltipContentWidth, -1f),
		};
		ApplyTextStyle(label, RegularFont, _bodyColor, rich: true);
		AppendFormattedText(label, template);
		return label;
	}

	private void AppendFormattedText(RichTextLabel label, string template)
	{
		var cursor = 0;
		while (cursor < template.Length)
		{
			var tokenStart = template.IndexOf("[icon:", cursor, StringComparison.Ordinal);
			if (tokenStart < 0)
			{
				AddColoredText(label, template[cursor..], _bodyColor);
				break;
			}

			AddColoredText(label, template[cursor..tokenStart], _bodyColor);
			var tokenEnd = template.IndexOf(']', tokenStart);
			if (tokenEnd < 0)
				throw new FormatException($"Tooltip icon token is missing a closing bracket: {template[tokenStart..]}");

			var token = template[(tokenStart + 6)..tokenEnd].Trim();
			var textSeparator = token.IndexOf(", text:", StringComparison.Ordinal);
			var iconName = (textSeparator >= 0 ? token[..textSeparator] : token).Trim();
			var icon = HudTooltipIconCatalog.Resolve(iconName);
			var texture = GD.Load<Texture2D>(icon.Path)
				?? throw new InvalidOperationException($"Could not load tooltip icon '{icon.Path}'.");
			var iconModulate = icon.TintIcon ? icon.Color : Colors.White;
			label.AddImage(texture, TooltipIconSize, TooltipIconSize, iconModulate);

			if (textSeparator >= 0)
				AddColoredText(label, token[(textSeparator + 7)..].TrimStart(), icon.Color);

			cursor = tokenEnd + 1;
		}
	}

	private static void ApplyTextStyle(Control control, Font font, Color color, bool rich)
	{
		control.AddThemeFontOverride(rich ? "normal_font" : "font", font);
		control.AddThemeFontSizeOverride(rich ? "normal_font_size" : "font_size", BodyFontSize);
		control.AddThemeColorOverride(rich ? "default_color" : "font_color", color);
	}

	private static void AddColoredText(RichTextLabel label, string text, Color color)
	{
		if (text.Length == 0)
			return;

		label.PushColor(color);
		label.AddText(text);
		label.Pop();
	}
}
