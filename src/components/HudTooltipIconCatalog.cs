using Godot;

namespace GrimSpace.Components;

public readonly record struct HudTooltipIconDef(string Path, Color Color);

public static class HudTooltipIconCatalog
{
	private static readonly Dictionary<string, HudTooltipIconDef> Icons = new(StringComparer.Ordinal)
	{
		["Damage"] = new("res://assets/ui/battle/damage.png", new Color(1f, 0.06f, 0.015f)),
		["Hull"] = new("res://assets/ui/battle/hull.png", new Color(0.82f, 0.08f, 0.16f)),
		["Shield"] = new("res://assets/ui/battle/shield.png", new Color(0.12f, 0.58f, 0.94f)),
		["Heat"] = new("res://assets/ui/battle/heat.png", new Color(0.78f, 0.3f, 0.08f)),
		["Overdrive"] = new("res://assets/ui/battle/heat.png", new Color(1f, 0.42f, 0.1f)),
		["Overheat"] = new("res://assets/ui/battle/heat.png", new Color(0.04f, 0.58f, 1f)),
		["ActionPoints"] = new("res://assets/ui/battle/action_points.svg", new Color(0.78f, 0.12f, 0.27f)),
		["ManeuverPoints"] = new("res://assets/ui/battle/maneuver_points.png", new Color(0.4f, 0.3f, 0.62f)),
		["Cooldown"] = new("res://assets/ui/battle/cooldown.png", new Color(0.2f, 0.38f, 0.68f)),
		["Credits"] = new("res://assets/ui/resources/credits.svg", new Color(0.98f, 0.82f, 0.14f)),
		["ScrapAlloy"] = new("res://assets/ui/resources/scrap-alloy.svg", new Color(0.7f, 0.78f, 0.82f)),
		["IndustrialCore"] = new("res://assets/ui/resources/industrial-core.svg", new Color(0.55f, 0.78f, 1f)),
		["Fuel"] = new("res://assets/ui/resources/fuel.png", new Color(0.48f, 0.82f, 0.82f)),
		["Salary"] = new("res://assets/ui/economy/day-symbol.png", new Color(0.98f, 0.82f, 0.14f)),
		["HazardPay"] = new("res://assets/ui/economy/hazard-swords-symbol.png", new Color(0.88f, 0.32f, 0.42f)),
	};

	public static HudTooltipIconDef Resolve(string name)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);
		if (!Icons.TryGetValue(name.Trim(), out var def))
			throw new FormatException($"Unknown tooltip icon '{name}'.");
		return def;
	}
}
