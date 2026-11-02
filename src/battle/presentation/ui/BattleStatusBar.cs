using Godot;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Statuses;
using GrimSpace.Components;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class BattleStatusBar : HFlowContainer
{
	private const float IconSize = 40f;
	private const string FramePath = "res://assets/ui/components/runtime/status_square_frame_neutral.png";
	private readonly Dictionary<string, StatusIcon> _icons = new(StringComparer.Ordinal);
	private IReadOnlyList<BattleStatusDisplay> _display = [];

	public BattleStatusBar()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		Visible = false;
		AddThemeConstantOverride("h_separation", 6);
		AddThemeConstantOverride("v_separation", 6);
	}

	public void Apply(UnitDisplayState unit)
	{
		var display = BattleStatusDisplay.Capture(unit);
		if (_display.SequenceEqual(display))
			return;

		var keys = display.Select(entry => entry.Key).ToHashSet(StringComparer.Ordinal);
		foreach (var key in _icons.Keys.Where(key => !keys.Contains(key)).ToArray())
		{
			var icon = _icons[key];
			RemoveChild(icon.Host);
			icon.Host.QueueFree();
			_icons.Remove(key);
		}

		for (var i = 0; i < display.Count; i++)
		{
			var entry = display[i];
			if (!_icons.TryGetValue(entry.Key, out var icon))
			{
				icon = CreateIcon(entry);
				_icons.Add(entry.Key, icon);
				AddChild(icon.Host);
			}

			MoveChild(icon.Host, i);
			icon.Host.SetTooltip(entry.Tooltip, HudPalettes.PurpleRed);
			icon.Counter.Visible = entry.StackCount.HasValue;
			icon.Counter.Text = entry.StackCount?.ToString() ?? string.Empty;
		}

		Visible = display.Count > 0;
		_display = display;
	}

	private static StatusIcon CreateIcon(BattleStatusDisplay entry)
	{
		var host = new HudTooltipHost
		{
			Name = entry.Key.Replace(':', '_'),
			CustomMinimumSize = new Vector2(IconSize, IconSize),
			MouseFilter = MouseFilterEnum.Stop,
		};
		var border = entry.Polarity == EStatusPolarity.Debuff
			? new Color(0.95f, 0.25f, 0.3f)
			: HudPalettes.PurpleRed.Primary;
		var material = new ShaderMaterial
		{
			Shader = GD.Load<Shader>("res://assets/ui/components/runtime/palette_remap.gdshader")
				?? throw new InvalidOperationException("Could not load UI palette shader."),
		};
		material.SetShaderParameter("surface_color", HudPalettes.PurpleRed.DeepSurface);
		material.SetShaderParameter("edge_color", border);
		material.SetShaderParameter("glow_color", border);
		material.SetShaderParameter("glow_strength", 0f);
		var frame = new TextureRect
		{
			Texture = GD.Load<Texture2D>(FramePath)
				?? throw new InvalidOperationException($"Could not load status frame '{FramePath}'."),
			Material = material,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		frame.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		host.AddChild(frame);
		var icon = HudTooltipIconCatalog.Resolve(entry.IconName);
		var glyph = new TextureRect
		{
			Texture = icon.TintIcon
				? SvgIconLoader.LoadCentered(icon.Path, icon.Color, 30)
				: SvgIconLoader.LoadRawCentered(icon.Path, 30),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		glyph.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		glyph.OffsetLeft = 6f;
		glyph.OffsetTop = 6f;
		glyph.OffsetRight = -6f;
		glyph.OffsetBottom = -6f;
		host.AddChild(glyph);
		var counter = new Label
		{
			AnchorLeft = 1f,
			AnchorTop = 1f,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			OffsetLeft = -28f,
			OffsetTop = -18f,
			OffsetRight = -2f,
			OffsetBottom = -1f,
			GrowHorizontal = GrowDirection.Begin,
			GrowVertical = GrowDirection.Begin,
			HorizontalAlignment = HorizontalAlignment.Right,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		counter.AddThemeFontSizeOverride("font_size", 12);
		counter.AddThemeColorOverride("font_color", Colors.White);
		counter.AddThemeColorOverride("font_outline_color", HudPalettes.PurpleRed.DeepSurface);
		counter.AddThemeConstantOverride("outline_size", 4);
		host.AddChild(counter);
		return new StatusIcon(host, counter);
	}

	private sealed record StatusIcon(HudTooltipHost Host, Label Counter);
}
