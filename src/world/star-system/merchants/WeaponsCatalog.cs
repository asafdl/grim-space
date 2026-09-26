using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Merchants;

public static class WeaponsCatalog
{
	private static readonly EAbilityKind[] SellableKinds =
		[EAbilityKind.Flak, EAbilityKind.Railgun];

	public static IReadOnlyList<MerchantCatalog.Offer> ListFor(ShipInstance ship)
	{
		ArgumentNullException.ThrowIfNull(ship);
		var offers = new List<MerchantCatalog.Offer>();
		var installedFlak = ship.Loadout.InstalledAbilities.Count(
			installed => installed.Mount.Kind == EAbilityKind.Flak);

		foreach (var slot in ship.Spec.Slots)
		{
			if (!SellableKinds.Contains(slot.Baseline.Kind))
				continue;

			var mount = slot.Mount;
			if (ship.Loadout.InstalledAbilities.Any(installed => installed.Mount == mount))
				continue;

			var offering = new MerchantCatalog.Offering(MerchantCatalog.Kind.InstallWeapon, mount);
			if (!MerchantShipChanges.TryPrepareAfter(offering, ship, out _))
				continue;

			var cost = mount.Kind switch
			{
				EAbilityKind.Flak => MerchantUpgradePricing.FlakInstall(installedFlak),
				EAbilityKind.Railgun => MerchantUpgradePricing.RailgunInstall(),
				_ => MerchantUpgradePricing.WeaponDamageUpgrade(0),
			};
			offers.Add(new MerchantCatalog.Offer(offering, cost));
		}

		foreach (var installed in ship.Loadout.InstalledAbilities)
		{
			var damageOffering = new MerchantCatalog.Offering(
				MerchantCatalog.Kind.UpgradeDamage,
				installed.Mount);
			if (MerchantShipChanges.TryPrepareAfter(damageOffering, ship, out _))
			{
				offers.Add(new MerchantCatalog.Offer(
					damageOffering,
					MerchantUpgradePricing.WeaponDamageUpgrade(installed.Spec.DamageUpgradeTier)));
			}

			var rangeOffering = new MerchantCatalog.Offering(
				MerchantCatalog.Kind.UpgradeRange,
				installed.Mount);
			if (MerchantShipChanges.TryPrepareAfter(rangeOffering, ship, out _))
			{
				offers.Add(new MerchantCatalog.Offer(
					rangeOffering,
					MerchantUpgradePricing.WeaponRangeUpgrade(installed.Spec.RangeUpgradeTier)));
			}
		}

		return offers;
	}
}
