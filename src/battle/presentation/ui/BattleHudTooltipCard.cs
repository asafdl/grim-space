using Godot;
using GrimSpace.Components;

namespace GrimSpace.Battle.Presentation.Ui;

internal sealed partial class BattleHudTooltipCard : PanelContainer
{
	private const float CornerCut = 12f;
	private const float TooltipIconSize = 16f;
	private const float TooltipContentWidth = 360f;
	private const float PartSeparation = 4f;
	private const int BodyFontSize = 18;
	private const string RegularFontPath = "res://assets/ui/fonts/Quantico-Regular.ttf";
	private const string BoldFontPath = "res://assets/ui/fonts/Quantico-Bold.ttf";
	private static readonly Color FillColor = new(0.035f, 0.025f, 0.055f, 0.98f);
	private static readonly Color BorderColor = HudPalettes.WithAlpha(HudPalettes.PurpleRed.Highlight, 0.9f);
	private static readonly Color BodyColor = new(0.88f, 0.84f, 0.92f);
	private static readonly Color HeatColor = new(0.78f, 0.3f, 0.08f);
	private static readonly Color OverdriveColor = new(1f, 0.42f, 0.1f);
	private static readonly Color ActionPointsColor = new(0.78f, 0.12f, 0.27f);
	private static readonly Color ManeuverPointsColor = new(0.4f, 0.3f, 0.62f);
	private static readonly Color CooldownColor = new(0.2f, 0.38f, 0.68f);
	private static readonly Color DamageColor = new(1f, 0.06f, 0.015f);
	private static readonly Color HullColor = new(0.82f, 0.08f, 0.16f);
	private static readonly Color ShieldColor = new(0.12f, 0.58f, 0.94f);
	private static readonly Font RegularFont = GD.Load<FontFile>(RegularFontPath);
	private static readonly Font BoldFont = GD.Load<FontFile>(BoldFontPath);

	private BattleHudTooltipCard(BattleHudTooltipContent tooltip)
	{
		MouseFilter = MouseFilterEnum.Ignore;
		Visible = true;
		AddThemeStyleboxOverride("panel", new StyleBoxEmpty
		{
			ContentMarginLeft = 8f,
			ContentMarginTop = 6f,
			ContentMarginRight = 8f,
			ContentMarginBottom = 6f,
		});

		var content = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		content.AddThemeConstantOverride("separation", 8);
		AddChild(content);

		if (!string.IsNullOrWhiteSpace(tooltip.Title))
			content.AddChild(CreateLabel(tooltip.Title, BoldFont, BodyFontSize, HudPalettes.PurpleRed.Title));

		if (!string.IsNullOrWhiteSpace(tooltip.Details))
			content.AddChild(CreateFormattedText(tooltip.Details));

		if (!string.IsNullOrWhiteSpace(tooltip.Body))
		{
			if (ContainsFormatToken(tooltip.Body))
				content.AddChild(CreateFormattedText(tooltip.Body));
			else
				content.AddChild(CreateWrappingBody(tooltip.Body));
		}

		CustomMinimumSize = GetCombinedMinimumSize();
	}

	public static Control Create(BattleHudTooltipContent tooltip) =>
		new BattleHudTooltipCard(tooltip);

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
			HudPalettes.PurpleRed.Title,
			2f,
			true);
	}

	private static Label CreateLabel(string text, Font font, int fontSize, Color color)
	{
		var label = new Label
		{
			Text = text,
			Visible = true,
			MouseFilter = MouseFilterEnum.Ignore,
			AutowrapMode = TextServer.AutowrapMode.Off,
		};
		label.AddThemeFontOverride("font", font);
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeColorOverride("font_color", color);
		return label;
	}

	private static Label CreateWrappingBody(string text)
	{
		var body = CreateLabel(text, RegularFont, BodyFontSize, BodyColor);
		body.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		body.CustomMaximumSize = new Vector2(TooltipContentWidth, -1f);
		return body;
	}

	private static VBoxContainer CreateFormattedText(string template)
	{
		var block = new VBoxContainer
		{
			Visible = true,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		block.AddThemeConstantOverride("separation", 4);
		foreach (var paragraph in template.Split('\n'))
		{
			foreach (var row in WrapParts(ParseParts(paragraph)))
				block.AddChild(CreateRow(row));
		}
		return block;
	}

	private static HBoxContainer CreateRow(IReadOnlyList<Part> parts)
	{
		var row = new HBoxContainer
		{
			Visible = true,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		row.AddThemeConstantOverride("separation", (int)PartSeparation);
		foreach (var part in parts)
		{
			if (part.Icon is { } icon)
				row.AddChild(CreateMetricIcon(icon));
			if (!string.IsNullOrEmpty(part.Text))
				row.AddChild(CreateLabel(part.Text, RegularFont, BodyFontSize, part.Color));
		}
		return row;
	}

	private static List<List<Part>> WrapParts(IReadOnlyList<Part> parts)
	{
		var rows = new List<List<Part>>();
		var current = new List<Part>();
		var used = 0f;

		foreach (var part in parts)
		{
			if (part.Icon is null && string.IsNullOrEmpty(part.Text))
				continue;

			if (part.Icon is not null)
			{
				AppendPart(rows, ref current, ref used, part, TooltipIconSize);
				continue;
			}

			foreach (var word in SplitPreserveSpaces(part.Text))
			{
				var width = MeasureText(word);
				AppendPart(rows, ref current, ref used, part with { Text = word }, width);
			}
		}

		if (current.Count > 0)
			rows.Add(current);
		if (rows.Count == 0)
			rows.Add([]);
		return rows;
	}

	private static void AppendPart(
		List<List<Part>> rows,
		ref List<Part> current,
		ref float used,
		Part part,
		float width)
	{
		var gap = current.Count > 0 ? PartSeparation : 0f;
		if (current.Count > 0 && used + gap + width > TooltipContentWidth)
		{
			rows.Add(current);
			current = [];
			used = 0f;
			gap = 0f;
		}

		if (current.Count > 0
			&& part.Icon is null
			&& current[^1].Icon is null
			&& current[^1].Color == part.Color)
		{
			current[^1] = current[^1] with { Text = current[^1].Text + part.Text };
		}
		else
		{
			current.Add(part);
		}

		used += gap + width;
	}

	private static IEnumerable<string> SplitPreserveSpaces(string text)
	{
		if (text.Length == 0)
			yield break;

		var start = 0;
		for (var i = 0; i < text.Length; i++)
		{
			if (text[i] != ' ')
				continue;
			if (i > start)
				yield return text[start..i];
			var spaceEnd = i;
			while (spaceEnd < text.Length && text[spaceEnd] == ' ')
				spaceEnd++;
			yield return text[i..spaceEnd];
			start = spaceEnd;
			i = spaceEnd - 1;
		}

		if (start < text.Length)
			yield return text[start..];
	}

	private static float MeasureText(string text) =>
		RegularFont.GetStringSize(text, HorizontalAlignment.Left, -1, BodyFontSize).X;

	private static List<Part> ParseParts(string template)
	{
		var parts = new List<Part>();
		var cursor = 0;
		while (cursor < template.Length)
		{
			var tokenStart = template.IndexOf("[icon:", cursor, StringComparison.Ordinal);
			if (tokenStart < 0)
			{
				parts.Add(new Part(template[cursor..], BodyColor, null));
				break;
			}

			if (tokenStart > cursor)
				parts.Add(new Part(template[cursor..tokenStart], BodyColor, null));

			var tokenEnd = template.IndexOf(']', tokenStart);
			if (tokenEnd < 0)
				throw new FormatException($"Tooltip icon token is missing a closing bracket: {template[tokenStart..]}");

			var token = template[(tokenStart + 6)..tokenEnd].Trim();
			var textSeparator = token.IndexOf(", text:", StringComparison.Ordinal);
			var iconName = textSeparator >= 0 ? token[..textSeparator] : token;
			var icon = ParseIconToken(iconName);
			parts.Add(new Part(string.Empty, ResourceColor(icon), icon));
			if (textSeparator >= 0)
			{
				var value = token[(textSeparator + 7)..].TrimStart();
				parts.Add(new Part(value, ResourceColor(icon), null));
			}

			cursor = tokenEnd + 1;
		}
		return parts;
	}

	private static TextureRect CreateMetricIcon(Icon iconKind) =>
		new()
		{
			Texture = LoadIcon(iconKind),
			Visible = true,
			CustomMinimumSize = new Vector2(TooltipIconSize, TooltipIconSize),
			Size = new Vector2(TooltipIconSize, TooltipIconSize),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			Modulate = ResourceColor(iconKind),
			MouseFilter = MouseFilterEnum.Ignore,
		};

	private static bool ContainsFormatToken(string text) =>
		text.Contains("[icon:", StringComparison.Ordinal);

	private static Icon ParseIconToken(string name) =>
		Enum.TryParse<Icon>(name.Trim(), out var icon) && Enum.IsDefined(icon)
			? icon
			: throw new FormatException($"Unknown tooltip icon '{name}'.");

	private static Texture2D LoadIcon(Icon iconKind)
	{
		var iconPath = iconKind switch
		{
			Icon.Damage => "res://assets/ui/battle/damage.png",
			Icon.Hull => "res://assets/ui/battle/hull.png",
			Icon.Shield => "res://assets/ui/battle/shield.png",
			Icon.Heat => "res://assets/ui/battle/heat.png",
			Icon.Overdrive or Icon.Overheat =>
				"res://assets/ui/battle/heat.png",
			Icon.ActionPoints => "res://assets/ui/battle/action_points.svg",
			Icon.ManeuverPoints => "res://assets/ui/battle/maneuver_points.png",
			Icon.Cooldown => "res://assets/ui/battle/cooldown.png",
			_ => throw new ArgumentOutOfRangeException(nameof(iconKind), iconKind, null),
		};
		return GD.Load<Texture2D>(iconPath)
			?? throw new InvalidOperationException($"Could not load tooltip icon '{iconPath}'.");
	}

	private static Color ResourceColor(Icon icon) =>
		icon switch
		{
			Icon.Heat => HeatColor,
			Icon.Overdrive => OverdriveColor,
			Icon.Overheat => new Color(0.04f, 0.58f, 1f),
			Icon.ActionPoints => ActionPointsColor,
			Icon.ManeuverPoints => ManeuverPointsColor,
			Icon.Cooldown => CooldownColor,
			Icon.Damage => DamageColor,
			Icon.Hull => HullColor,
			Icon.Shield => ShieldColor,
			_ => throw new ArgumentOutOfRangeException(nameof(icon), icon, null),
		};

	private readonly record struct Part(string Text, Color Color, Icon? Icon);
}

internal sealed partial class BattleHudTooltipButton : Button
{
	private BattleHudTooltipContent? _tooltipContent;

	public void SetTooltip(BattleHudTooltipContent tooltip)
	{
		_tooltipContent = tooltip;
		TooltipText = tooltip.TriggerText;
	}

	public override Control _MakeCustomTooltip(string forText) =>
		BattleHudTooltipCard.Create(_tooltipContent ?? BattleHudTooltipContent.Plain(forText));
}
