using Godot;
using GrimSpace.Components;

namespace GrimSpace.Battle.Presentation.Ui;

internal static class BattleHudTooltipCard
{
	public static Control Create(BattleHudTooltipContent tooltip) =>
		HudTooltipCard.Create(tooltip, HudPalettes.PurpleRed);

	public static Control Create(HudTooltipContent tooltip) =>
		HudTooltipCard.Create(tooltip, HudPalettes.PurpleRed);
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
