using Godot;

namespace GrimSpace.Components;

/// <summary>
/// Drop-in Control that shows <see cref="HudTooltipCard"/> through Godot's native tooltip popup.
/// Does not change layout — size it like any other Control.
/// </summary>
public partial class HudTooltipHost : Control
{
	private static readonly Theme TooltipHostTheme =
		GD.Load<Theme>("res://assets/ui/themes/hud_tooltip_host_theme.tres")
		?? throw new InvalidOperationException("Could not load the shared tooltip host theme.");

	private HudTooltipContent _content = HudTooltipContent.Plain(string.Empty);
	private HudPalette _palette = HudPalettes.BlueCyan;

	public HudTooltipHost()
	{
		Theme = TooltipHostTheme;
	}

	public void SetTooltip(string text) =>
		SetTooltip(HudTooltipContent.Plain(text));

	public void SetTooltip(HudTooltipContent content, HudPalette? palette = null)
	{
		ArgumentNullException.ThrowIfNull(content);
		_content = content;
		if (palette is { } chosen)
			_palette = chosen;
		TooltipText = content.TriggerText;
	}

	public override Vector2 _GetMinimumSize()
	{
		var size = Vector2.Zero;
		foreach (var child in GetChildren())
		{
			if (child is Control { Visible: true } control)
				size = size.Max(control.GetCombinedMinimumSize());
		}
		return size;
	}

	public override Control _MakeCustomTooltip(string forText) =>
		HudTooltipCard.Create(_content, _palette);
}
