using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

internal static class MerchantOfferDisplay
{
	public static string PackTitle(MerchantCatalog.Offering offering) =>
		offering.Kind switch
		{
			MerchantCatalog.Kind.UpgradeDamage =>
				$"Damage upgrade pack · Level {offering.TargetLevel}",
			MerchantCatalog.Kind.UpgradeRange =>
				$"Range upgrade pack · Level {offering.TargetLevel}",
			MerchantCatalog.Kind.UpgradeMaxShields =>
				$"Shield upgrade pack · Level {offering.TargetLevel}",
			_ => throw new ArgumentOutOfRangeException(
				nameof(offering),
				offering,
				"Offering is not an upgrade pack."),
		};

	public static string AbilityUpgradeBody(
		MerchantCatalog.Offering offering,
		InstalledAbility installed)
	{
		var currentTier = offering.Kind switch
		{
			MerchantCatalog.Kind.UpgradeDamage => installed.DamageUpgradeTier,
			MerchantCatalog.Kind.UpgradeRange => installed.RangeUpgradeTier,
			_ => throw new ArgumentOutOfRangeException(
				nameof(offering),
				offering,
				"Offering is not an ability upgrade."),
		};
		return $"Upgrade level {currentTier} \u2192 {offering.TargetLevel}";
	}

	public static string ShieldUpgradeBody(
		ShipInstance ship,
		ESpatialOrientation face,
		int targetMaxShieldPoints)
	{
		var currentMax = ship.Loadout.MaxShieldPoints[face];
		return $"Max shields {currentMax} \u2192 {targetMaxShieldPoints}";
	}

	public static string HullRepairBody(int current, int max) =>
		$"Hull integrity {current} \u2192 {max}";

	public static string ShieldRechargeBody(int current, int max) =>
		$"Shield charge {current} \u2192 {max}";

	public static string RechargeAllBody(int current, int max) =>
		$"Total shields {current} \u2192 {max}";

	public static string BandLabel(EMerchantAvailabilityBand band) =>
		band switch
		{
			EMerchantAvailabilityBand.T0 => "Shop tier T0",
			EMerchantAvailabilityBand.T0_5 => "Shop tier T0.5",
			EMerchantAvailabilityBand.T1 => "Shop tier T1",
			EMerchantAvailabilityBand.T1_5 => "Shop tier T1.5",
			EMerchantAvailabilityBand.T2 => "Shop tier T2",
			_ => throw new ArgumentOutOfRangeException(nameof(band), band, null),
		};

	public static string FaceLabel(ESpatialOrientation face) =>
		face switch
		{
			ESpatialOrientation.Forward => "Forward",
			ESpatialOrientation.Retro => "Aft",
			ESpatialOrientation.Port => "Port",
			ESpatialOrientation.Starboard => "Starboard",
			ESpatialOrientation.Dorsal => "Dorsal",
			ESpatialOrientation.Ventral => "Ventral",
			_ => face.ToString(),
		};

	public static string KindLabel(EAbilityKind kind) =>
		kind switch
		{
			EAbilityKind.ScrapDroneSwarm => "scrap drone swarm",
			EAbilityKind.LightningCannon => "lightning cannon",
			EAbilityKind.MinerBay => "repurposed Miner bay",
			EAbilityKind.VoidBombLauncher => "VoidBomb launcher",
			EAbilityKind.GoopGun => "goop gun",
			_ => kind.ToString(),
		};
}
