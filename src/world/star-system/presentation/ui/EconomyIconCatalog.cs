using GrimSpace.Components;
using GrimSpace.World.StarSystem.Economy;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public static class EconomyIconCatalog
{
	public const string SalaryIconPath = "res://assets/ui/economy/day-symbol.png";
	public const string HazardPayIconPath = "res://assets/ui/economy/hazard-swords-symbol.png";

	public static HudTooltipContent SalaryTooltip { get; } =
		HudTooltipContent.Plain(
			$"Salary paid every {StrategicEconomyConfig.SalaryCadenceMinutes} game minutes");

	public static HudTooltipContent HazardPayTooltip { get; } =
		HudTooltipContent.Plain("Hazard pay per battle");
}
