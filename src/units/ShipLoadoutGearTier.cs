using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Units;

public static class ShipLoadoutGearTier
{
	public static EShipGearTier FromLoadout(ShipLoadout loadout, EType chassis)
	{
		ArgumentNullException.ThrowIfNull(loadout);
		var spent = SpentUpgradeBudget(loadout, ShipCatalog.LoadoutForTier(chassis, EShipGearTier.T0));
		return TierForSpentBudget(spent);
	}

	public static EShipGearTier FromLoadout(ShipLoadout loadout, ShipLoadout baseline)
	{
		ArgumentNullException.ThrowIfNull(loadout);
		ArgumentNullException.ThrowIfNull(baseline);
		return TierForSpentBudget(SpentUpgradeBudget(loadout, baseline));
	}

	internal static int SpentUpgradeBudget(ShipLoadout loadout, ShipLoadout baseline)
	{
		var spent = 0;
		spent += (loadout.HullUpgradeTier - baseline.HullUpgradeTier) * ShipLoadoutTierRoller.CostHullMax;

		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
		{
			spent += (loadout.ShieldUpgradeTiers[face] - baseline.ShieldUpgradeTiers[face])
				* ShipLoadoutTierRoller.CostShieldMax;
		}

		var baselineByMount = baseline.InstalledAbilities.ToDictionary(installed => installed.Mount);
		foreach (var installed in loadout.InstalledAbilities)
		{
			if (!baselineByMount.TryGetValue(installed.Mount, out var baselineInstalled))
			{
				spent += ShipLoadoutTierRoller.CostInstallWeapon;
				continue;
			}

			spent += (installed.Spec.DamageUpgradeTier - baselineInstalled.Spec.DamageUpgradeTier)
				* ShipLoadoutTierRoller.CostWeaponDamage;
			spent += (installed.Spec.RangeUpgradeTier - baselineInstalled.Spec.RangeUpgradeTier)
				* ShipLoadoutTierRoller.CostWeaponRange;
		}

		return spent;
	}

	private static EShipGearTier TierForSpentBudget(int spent)
	{
		if (spent <= 0)
			return EShipGearTier.T0;
		if (spent <= ShipLoadoutTierRoller.PowerBudget(EShipGearTier.T1))
			return EShipGearTier.T1;
		if (spent <= ShipLoadoutTierRoller.PowerBudget(EShipGearTier.T2))
			return EShipGearTier.T2;
		return EShipGearTier.T3;
	}
}
