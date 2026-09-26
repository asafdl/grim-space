using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Loadouts.Abilities;
namespace GrimSpace.World.StarSystem.Presentation.Facilities;

internal static class MerchantOfferDisplay
{
	public static string ShieldUpgradeTitle(ShipLoadout loadout, ESpatialOrientation face) =>
		$"{FacetLabel(face)} max shields {MkLabel(loadout.ShieldUpgradeTiers[face])}";

	public static string HullUpgradeTitle(ShipLoadout loadout) =>
		$"Max hull {MkLabel(loadout.HullUpgradeTier)}";

	public static string InstallTitle(AbilityMount mount) =>
		$"Install {KindLabel(mount.Kind)} ({FacetLabel(mount.Facet)})";

	public static string InstallBody(AbilitySpec spec) =>
		$"Mount a new {KindLabel(spec.Kind)} on an open facet.";

	public static string DamageUpgradeTitle(AbilityMount mount, AbilitySpec current) =>
		$"{FacetLabel(mount.Facet)} {KindLabel(mount.Kind)} damage {MkLabel(current.DamageUpgradeTier)}";

	public static string RangeUpgradeTitle(AbilityMount mount, AbilitySpec current) =>
		$"{FacetLabel(mount.Facet)} {KindLabel(mount.Kind)} range {MkLabel(current.RangeUpgradeTier)}";

	public static string DamageUpgradeBody(AbilitySpec current) =>
		current switch
		{
			FlakSpec flak =>
				$"Increase burst damage from {flak.Damage} to {flak.Damage + 1}.",
			RailgunSpec railgun =>
				$"Increase shot damage from {railgun.Damage} to {railgun.Damage + 1}.",
			_ => "Improve this mounted system.",
		};

	public static string RangeUpgradeBody(AbilitySpec current) =>
		current switch
		{
			FlakSpec flak =>
				$"Increase burst range from {flak.BurstRange} to {flak.BurstRange + 1}.",
			RailgunSpec railgun =>
				$"Increase line length from {railgun.LineLength} to {railgun.LineLength + 1}.",
			_ => "Extend this mounted system's reach.",
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

	private static string MkLabel(int upgradeTier) => $"Mk {upgradeTier + 1}";

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
