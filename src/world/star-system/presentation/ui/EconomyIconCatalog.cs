using GrimSpace.Components;
using GrimSpace.World.StarSystem.Economy;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public static class EconomyIconCatalog
{
	public const string SalaryIconPath = "res://assets/ui/economy/day-symbol.png";
	public const string HazardPayIconPath = "res://assets/ui/economy/hazard-swords-symbol.png";

	public static HudTooltipContent SalaryTooltip { get; } =
		new HudTooltipContent(
			"Salary",
			$"Paid every day");

	public static HudTooltipContent HazardPayTooltip { get; } =
		new HudTooltipContent(
			"Hazard Pay",
			"Mercs don't fight for free. Failure to pay might lead for pilots to refuse to fight.");

	public static HudTooltipContent FleeFuelTooltip { get; } =
		HudTooltipContent.Plain("Evasive manuevers");
}