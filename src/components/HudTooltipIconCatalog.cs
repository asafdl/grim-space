using Godot;

namespace GrimSpace.Components;

public readonly record struct HudTooltipIconDef(string Path, Color Color, bool TintIcon = true);

public static class HudTooltipIconCatalog
{
	private static readonly Dictionary<string, HudTooltipIconDef> Icons = new(StringComparer.Ordinal)
	{
		["Damage"] = new("res://assets/ui/battle/damage.png", new Color(1f, 0.06f, 0.015f)), // hex #ff0f03
		["Heal"] = new("res://assets/ui/battle/heal.png", new Color(0.45f, 0.92f, 0.38f), TintIcon: false), // hex #72e962
		["Hull"] = new("res://assets/ui/battle/hull.png", new Color(0.82f, 0.08f, 0.16f)), // hex #d2142a
		["Shield"] = new("res://assets/ui/battle/shield.png", new Color(0.12f, 0.58f, 0.94f)), // hex #1e93f0
		["Heat"] = new("res://assets/ui/battle/heat.png", new Color(0.78f, 0.3f, 0.08f)), // hex #c64d14
		["Overdrive"] = new("res://assets/ui/battle/heat.png", new Color(1f, 0.42f, 0.1f)), // hex #ff6a1a
		["Overheat"] = new("res://assets/ui/battle/heat.png", new Color(0.04f, 0.58f, 1f)), // hex #0a94ff
		["ActionPoints"] = new("res://assets/ui/battle/action_points.svg", new Color(0.78f, 0.12f, 0.27f)), // hex #c61e46
		["ManeuverPoints"] = new("res://assets/ui/battle/maneuver_points.png", new Color(0.4f, 0.3f, 0.62f)), // hex #664f9e
		["Cooldown"] = new("res://assets/ui/battle/cooldown.png", new Color(0.2f, 0.38f, 0.68f)), // hex #3361af
		["Credits"] = new("res://assets/ui/resources/credits.svg", new Color(0.98f, 0.82f, 0.14f)), // hex #feca22
		["ScrapAlloy"] = new("res://assets/ui/resources/scrap-alloy.svg", new Color(0.7f, 0.78f, 0.82f)), // hex #b3bfc4
		["IndustrialCore"] = new("res://assets/ui/resources/industrial-core.svg", new Color(0.55f, 0.78f, 1f)), // hex #8bbfe8
		["Fuel"] = new("res://assets/ui/resources/fuel.png", new Color(0.48f, 0.82f, 0.82f)), // hex #7cc9c9
		["Salary"] = new("res://assets/ui/economy/day-symbol.png", new Color(0.98f, 0.82f, 0.14f)), // hex #feca22
		["HazardPay"] = new("res://assets/ui/economy/hazard-swords-symbol.png", new Color(0.88f, 0.32f, 0.42f)), // hex #e0526a
	};

	public static HudTooltipIconDef Resolve(string name)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);
		if (!Icons.TryGetValue(name.Trim(), out var def))
			throw new FormatException($"Unknown tooltip icon '{name}'.");
		return def;
	}
}
