using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.World.StarSystem.Dockyard;

public static class DockyardUpgradeDisplay
{
	public static string ShieldUpgradeTitle(ShipSpec spec) =>
		$"Max shields {DockyardUpgradeRules.MkLabel(spec.ShieldUpgradeTier + 1)}";

	public static string ShieldUpgradeBody(ShipSpec spec) =>
		$"Raise maximum shield capacity on every face (current {DockyardUpgradeRules.MkLabel(spec.ShieldUpgradeTier)}).";

	public static string AbilityUpgradeTitle(AbilityMount mount, AbilitySpec current) =>
		$"{FacetLabel(mount.Facet)} {KindLabel(mount.Kind)} {DockyardUpgradeRules.MkLabel(AbilitySpecUpgrade.Tier(current) + 1)}";

	public static string AbilityUpgradeBody(AbilitySpec current) =>
		current switch
		{
			FlakSpec flak =>
				$"Increase burst damage from {flak.Damage} to {flak.Damage + 1}.",
			RailgunSpec railgun =>
				$"Increase shot damage from {railgun.Damage} to {railgun.Damage + 1}.",
			_ => "Improve this mounted system.",
		};

	public static string MountedAbilitySummary(InstalledAbility installed) =>
		$"{FacetLabel(installed.MountedOn)} · {KindLabel(installed.Spec.Kind)} · {DockyardUpgradeRules.MkLabel(AbilitySpecUpgrade.Tier(installed.Spec))} · {AbilityStatSummary(installed.Spec)}";

	private static string AbilityStatSummary(AbilitySpec spec) =>
		spec switch
		{
			FlakSpec flak => $"damage {flak.Damage}",
			RailgunSpec railgun => $"damage {railgun.Damage}",
			PatrolBaySpec bay => $"cooldown {bay.CooldownTurns}t",
			TorpedoLauncherSpec launcher => $"cooldown {launcher.CooldownTurns}t",
			_ => spec.Kind.ToString(),
		};

	private static string FacetLabel(ESpatialOrientation facet) =>
		facet switch
		{
			ESpatialOrientation.Forward => "Forward",
			ESpatialOrientation.Retro => "Aft",
			ESpatialOrientation.Port => "Port",
			ESpatialOrientation.Starboard => "Starboard",
			ESpatialOrientation.Dorsal => "Dorsal",
			ESpatialOrientation.Ventral => "Ventral",
			_ => facet.ToString(),
		};

	private static string KindLabel(EAbilityKind kind) =>
		kind switch
		{
			EAbilityKind.Flak => "Flak",
			EAbilityKind.Railgun => "Railgun",
			EAbilityKind.PatrolBay => "Patrol bay",
			EAbilityKind.TorpedoLauncher => "Torpedo launcher",
			_ => kind.ToString(),
		};
}
