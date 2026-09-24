using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

internal static class MerchantOfferDisplay
{
	public static string ShieldUpgradeTitle(ShipSpec spec) =>
		$"Max shields {MerchantPricingRules.MkLabel(spec.ShieldUpgradeTier + 1)}";

	public static string AbilityUpgradeTitle(AbilityMount mount, AbilitySpec current) =>
		$"{FacetLabel(mount.Facet)} {KindLabel(mount.Kind)} {MerchantPricingRules.MkLabel(current.UpgradeTier + 1)}";

	public static string AbilityUpgradeBody(AbilitySpec current) =>
		current switch
		{
			FlakSpec flak =>
				$"Increase burst damage from {flak.Damage} to {flak.Damage + 1}.",
			RailgunSpec railgun =>
				$"Increase shot damage from {railgun.Damage} to {railgun.Damage + 1}.",
			_ => "Improve this mounted system.",
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
