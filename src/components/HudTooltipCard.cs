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
	private bool _interactiveIcons;
	private IReadOnlyDictionary<string, HudTooltipContent>? _iconTooltips;
	private string? _hint;
	private string? _presentedText;

	public event Action<string>? IconHoverStarted;
	public event Action<string>? IconHoverEnded;

	public HudTooltipCard()
		: this(HudPalettes.BlueCyan)
	{
	}

	private HudTooltipCard(HudPalette palette, bool nested = false)
	{
		_palette = palette;
		_fillColor = HudPalettes.WithAlpha(palette.Surface, 0.98f);
		_borderColor = HudPalettes.WithAlpha(nested ? palette.Secondary : palette.Highlight, 0.9f);
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

	public static HudTooltipCard Create(
		HudTooltipContent tooltip,
		HudPalette palette,
		bool pinned = false,
		string? headerIcon = null)
	{
		ArgumentNullException.ThrowIfNull(tooltip);
		var card = new HudTooltipCard(palette, nested: headerIcon is not null);
		if (!pinned)
		{
			card._hint = tooltip.Comparison is null
				? "Hold Alt to pin."
				: "Hold Shift to compare. Hold Alt to pin.";
			card.AddChild(new HudTooltipPin(tooltip, palette));
		}
		else if (headerIcon is null
			&& (HudIconMarkup.Contains(tooltip.Body)
				|| (tooltip.Details is not null && HudIconMarkup.Contains(tooltip.Details))
				|| (tooltip.Rows?.Any(row => row.IconName is not null) ?? false)
				|| (tooltip.InlineRows?.Any(row => row.IconName is not null) ?? false)))
		{
			card._interactiveIcons = true;
			card._iconTooltips = tooltip.IconTooltips;
			card.MouseFilter = MouseFilterEnum.Stop;
			card._content.MouseFilter = MouseFilterEnum.Pass;
		}
		card.BuildContent(tooltip, headerIcon);
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

	private void BuildContent(HudTooltipContent tooltip, string? headerIcon = null)
	{
		foreach (var child in _content.GetChildren())
		{
			_content.RemoveChild(child);
			child.QueueFree();
		}

		if (headerIcon is not null)
			_content.AddChild(CreateFormattedText($"[icon: {headerIcon}]"));
		else if (!string.IsNullOrWhiteSpace(tooltip.Title))
			_content.AddChild(CreateTitle(tooltip.Title));
		AddSections(tooltip.Details);
		AddSections(tooltip.Body);
		if (tooltip.InlineRows is not null)
			foreach (var inline in tooltip.InlineRows)
			{
				var prefix = inline.IconName is null ? "" : $"[icon: {inline.IconName}] ";
				var line = CreateFormattedText(prefix);
				if (inline.IconName is not null)
					line.PushColor(HudTooltipIconCatalog.Resolve(inline.IconName).Color);
				line.AutowrapMode = TextServer.AutowrapMode.Off;
				line.CustomMaximumSize = new Vector2(-1, -1);
				var text = new System.Text.StringBuilder(inline.IconName is null ? "" : " ");
				foreach (var row in inline.Values)
				{
					var value = $"{row.Label}:{row.Value}";
					if (text.Length > (inline.IconName is null ? 0 : 1))
					{
						line.AddText(" ");
						text.Append(' ');
					}
					line.AddText(value);
					text.Append(value);
					text.Append(AppendDelta(line, row));
				}
				if (inline.IconName is not null)
					line.Pop();
				line.CustomMinimumSize = new Vector2(
					RegularFont.GetStringSize(text.ToString(), HorizontalAlignment.Left, -1, BodyFontSize).X
						+ (inline.IconName is null ? 0 : TooltipIconSize),
					0);
				_content.AddChild(line);
			}
		if (tooltip.Rows is not null)
		{
			var grid = new GridContainer
			{
				Columns = tooltip.TwoColumnRows ? 2 : 1,
				MouseFilter = MouseFilterEnum.Ignore,
			};
			grid.AddThemeConstantOverride("h_separation", 16);
			_content.AddChild(grid);
			foreach (var row in tooltip.Rows)
			{
				var prefix = row.IconName is null ? "" : $"[icon: {row.IconName}] ";
				var line = CreateFormattedText(prefix);
				if (row.IconName is not null)
					line.PushColor(HudTooltipIconCatalog.Resolve(row.IconName).Color);
				line.AddText($"{row.Label}: {row.Value}");
				if (row.IconName is not null)
					line.Pop();
				AppendDelta(line, row);
				grid.AddChild(line);
			}
		}
		AddSections(_hint);
	}

	private static string AppendDelta(RichTextLabel line, HudTooltipRow row)
	{
		if (row.Delta is not { } delta || delta == 0)
			return "";
		var text = $" [{(delta > 0 ? "+" : "")}{delta}{row.DeltaSuffix}]";
		line.PushColor(delta > 0 ? Color.FromHtml("#46da0c") : Color.FromHtml("#f72657"));
		line.AddText(text);
		line.Pop();
		return text;
	}

	private void AddSections(string? template)
	{
		if (string.IsNullOrWhiteSpace(template))
			return;

		var sections = template
			.Split(HudTooltipContent.Divider)
			.Select(section => section.Trim('\n'))
			.Where(section => !string.IsNullOrWhiteSpace(section))
			.ToArray();
		for (var i = 0; i < sections.Length; i++)
		{
			if (i > 0)
				_content.AddChild(new ColorRect
				{
					MouseFilter = MouseFilterEnum.Ignore,
					CustomMinimumSize = new Vector2(0f, 1f),
					Color = HudPalettes.WithAlpha(_palette.Highlight, 0.45f),
				});
			_content.AddChild(CreateFormattedText(sections[i]));
		}
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
			MouseFilter = _interactiveIcons ? MouseFilterEnum.Pass : MouseFilterEnum.Ignore,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			FitContent = true,
			ScrollActive = false,
			CustomMaximumSize = new Vector2(TooltipContentWidth, -1f),
		};
		ApplyTextStyle(label, RegularFont, _bodyColor, rich: true);
		HudIconMarkup.Append(label, template, _bodyColor, TooltipIconSize, _interactiveIcons, _iconTooltips);
		if (_interactiveIcons)
		{
			var hoverArea = new Control { MouseFilter = MouseFilterEnum.Stop };
			label.AddChild(hoverArea);
			hoverArea.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
			var hoveredIcon = string.Empty;
			void UpdateHover(string icon)
			{
				if (hoveredIcon == icon)
					return;
				if (hoveredIcon.Length > 0)
					IconHoverEnded?.Invoke(hoveredIcon);
				hoveredIcon = icon;
				if (hoveredIcon.Length > 0)
					IconHoverStarted?.Invoke(hoveredIcon);
			}
			hoverArea.MouseEntered += () => UpdateHover(label.GetTooltip(label.GetLocalMousePosition()));
			hoverArea.MouseExited += () => UpdateHover(string.Empty);
			hoverArea.GuiInput += input =>
			{
				if (input is InputEventMouseMotion)
					UpdateHover(label.GetTooltip(label.GetLocalMousePosition()));
			};
		}
		return label;
	}

	private static void ApplyTextStyle(Control control, Font font, Color color, bool rich)
	{
		control.AddThemeFontOverride(rich ? "normal_font" : "font", font);
		control.AddThemeFontSizeOverride(rich ? "normal_font_size" : "font_size", BodyFontSize);
		control.AddThemeColorOverride(rich ? "default_color" : "font_color", color);
	}
}
