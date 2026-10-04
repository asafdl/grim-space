using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

internal static class MerchantOfferDisplay
{
	internal sealed record AbilityEntry(
		AbilityMount Mount,
		bool IsInstalled,
		IReadOnlyList<MerchantCatalog.Offer> Offers);

	public static AbilityEntry[] AbilitiesFor(ShipInstance ship, IReadOnlyList<MerchantCatalog.Offer> offers)
	{
		var installed = ship.Loadout.InstalledAbilities.Select(ability => ability.Mount).ToHashSet();
		return offers
			.Select(offer => offer.Offering.Mount)
			.OfType<AbilityMount>()
			.Concat(installed)
			.Distinct()
			.OrderBy(mount => mount.Facet)
			.ThenBy(mount => mount.Kind)
			.Select(mount => new AbilityEntry(
				mount,
				installed.Contains(mount),
				offers.Where(offer => offer.Offering.Mount == mount).ToArray()))
			.ToArray();
	}

	public static string AbilityIconPath(EAbilityKind kind) =>
		kind switch
		{
			EAbilityKind.ScrapDroneSwarm => "res://assets/ui/abilities/scrap_drone_swarm.svg",
			EAbilityKind.LightningCannon => "res://assets/ui/abilities/lightning_cannon.svg",
			EAbilityKind.MinerBay => "res://assets/ui/abilities/repurposed-miner.svg",
			EAbilityKind.VoidBombLauncher => "res://assets/ui/abilities/void_bomb.svg",
			_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown ability icon."),
		};

	public static string CapacityUpgradeTitle(int currentTier) =>
		$"Capacity \u00b7 {MkLabel(currentTier)}";

	public static string HullRepairBody(int current, int max) =>
		$"Hull integrity {current} \u2192 {max}";

	public static string HullCapacityBody(int currentMax) =>
		$"Max hull {currentMax} \u2192 {currentMax + 1}";

	public static string ShieldRechargeBody(int current, int max) =>
		$"Shield charge {current} \u2192 {max}";

	public static string ShieldCapacityBody(int currentMax) =>
		$"Max shields {currentMax} \u2192 {currentMax + 1}";

	public static string RechargeAllBody(int current, int max) =>
		$"Total shields {current} \u2192 {max}";

	public const string InstallTitle = "Install ability";
	public const string InstallBody = "Add this ability to the selected mount.";

	public static string DamageUpgradeTitle(AbilitySpec current) =>
		$"Damage \u00b7 {MkLabel(current.DamageUpgradeTier)}";

	public static string RangeUpgradeTitle(AbilitySpec current) =>
		$"Range \u00b7 {MkLabel(current.RangeUpgradeTier)}";

	public static string DamageUpgradeBody(AbilitySpec current) =>
		current switch
		{
			ScrapDroneSwarmSpec swarm =>
				$"Burst damage {swarm.Damage} \u2192 {swarm.Damage + 1}",
			LightningCannonSpec lightningCannon =>
				$"Shot damage {lightningCannon.Damage} \u2192 {lightningCannon.Damage + 1}",
			_ => "Improve this mounted system.",
		};

	public static string RangeUpgradeBody(AbilitySpec current) =>
		current switch
		{
			ScrapDroneSwarmSpec swarm =>
				$"Burst range {swarm.BurstRange} \u2192 {swarm.BurstRange + 1} cells",
			LightningCannonSpec lightningCannon =>
				$"Line length {lightningCannon.LineLength} \u2192 {lightningCannon.LineLength + 1} cells",
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

	public static string KindLabel(EAbilityKind kind) =>
		kind switch
		{
			EAbilityKind.ScrapDroneSwarm => "Scrap drone swarm",
			EAbilityKind.LightningCannon => "Lightning cannon",
			EAbilityKind.MinerBay => "Repurposed Miner bay",
			EAbilityKind.VoidBombLauncher => "VoidBomb launcher",
			_ => kind.ToString(),
		};
}
