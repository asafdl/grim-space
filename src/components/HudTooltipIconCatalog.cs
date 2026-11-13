using Godot;

namespace GrimSpace.Components;

public readonly record struct HudTooltipIconDef(string Path, Color Color, string? Description = null, bool TintIcon = true)
{
	public HudTooltipContent? Tooltip(
		string name,
		IReadOnlyDictionary<string, HudTooltipContent>? overrides = null)
	{
		if (overrides is not null && overrides.TryGetValue(name, out var tooltip))
			return tooltip;
		return string.IsNullOrWhiteSpace(Description) ? null : HudTooltipContent.Plain(Description);
	}
}

public static class HudTooltipIconCatalog
{
	private static readonly Dictionary<string, HudTooltipIconDef> Icons = new(StringComparer.Ordinal)
	{
		["Damage"] = new("res://assets/ui/battle/damage.png", new Color(1f, 0.06f, 0.015f), "Damage dealt to shields and hull."), // hex #ff0f03
		["Crit"] = new("res://assets/ui/battle/crit.png", new Color(1f, 0.84f, 0.35f), "Bonus damage multiplier.", TintIcon: false),
		["Heal"] = new("res://assets/ui/battle/heal.png", new Color(0.45f, 0.92f, 0.38f), "Restores shields, then hull.", TintIcon: false), // hex #72e962
		["Hull"] = new("res://assets/ui/battle/hull.png", new Color(0.82f, 0.08f, 0.16f), "At zero hull, the ship is destroyed."), // hex #d2142a
		["Shield"] = new("res://assets/ui/battle/shield.png", new Color(0.12f, 0.58f, 0.94f), "Absorbs damage on the hit face."), // hex #1e93f0
		["Heat"] = new("res://assets/ui/battle/heat.png", new Color(0.78f, 0.3f, 0.08f), "Ships accumulate heat by doing actions, at max heat, enter overdrive, then for next turn overheat."), // hex #c64d14
		["Overdrive"] = new("res://assets/ui/battle/heat.png", new Color(1f, 0.68f, 0.1f), "At max heat, get a damage bonus for all further damage this turn, overheat next turn."), // hex #ffad1a
		["Overheat"] = new("res://assets/ui/battle/heat.png", new Color(0.04f, 0.58f, 1f), "Abilities are unavailable when overheated."), // hex #0a94ff
		["CurrentActionPoints"] = new("res://assets/ui/battle/action_points.svg", new Color(0.78f, 0.12f, 0.27f), "Action points, used for movement."), // hex #c61e46
		["CurrentManeuverPoints"] = new("res://assets/ui/battle/maneuver_points.png", new Color(0.4f, 0.3f, 0.62f), "Maneuver points, used for yaw, pitch, and roll."), // hex #664f9e
		["Range"] = new("res://assets/ui/battle/range.png", new Color(0.62f, 0.86f, 0.96f)), // hex #9edbf5
		["ScrapDroneSwarm"] = new("res://assets/ui/abilities/scrap_drone_swarm.svg", new Color(0.98f, 0.82f, 0.14f)), // hex #feca22
		["LightningCannon"] = new("res://assets/ui/abilities/lightning_cannon.svg", new Color(0.38f, 0.68f, 1f)), // hex #61adff
		["GoopGun"] = new("res://assets/ui/abilities/goop_gun.svg", new Color(0.42f, 0.88f, 0.38f)), // hex #6be061
		["VoidBombLauncher"] = new("res://assets/ui/abilities/void_bomb.svg", new Color(0.68f, 0.32f, 0.98f)), // hex #ad52fa
		["MinerBay"] = new("res://assets/ui/abilities/repurposed-miner.svg", new Color(0.4f, 0.9f, 0.58f)), // hex #66e594
		["Ram"] = new("res://assets/ui/abilities/ram.png", new Color(0.92f, 0.55f, 0.28f)), // hex #eb8c47
		["HighVelocityRod"] = new("res://assets/ui/abilities/high_velocity_rod.svg", new Color(0.2f, 0.95f, 0.9f)),
		["RoadsWeDontNeedRoads"] = new("res://assets/ui/abilities/roads_we_dont_need_roads.svg", new Color(0.65f, 0.45f, 1f)),
		["ProtectionDrone"] = new("res://assets/ui/abilities/protection_drone.png", new Color(0.55f, 0.85f, 0.95f)),
		["Cooldown"] = new("res://assets/ui/battle/cooldown.png", new Color(0.2f, 0.38f, 0.68f), "Ability cooldown."), // hex #3361af
		["KillReset"] = new("res://assets/ui/battle/kill_reset.png", Colors.White, "Resets cooldown on kill.", TintIcon: false),
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
