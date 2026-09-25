using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Merchants;

public static class WeaponsCatalog
{
	private static readonly EAbilityKind[] SellableKinds =
		[EAbilityKind.Flak, EAbilityKind.Railgun];

	private static readonly ResourceBundle FlakInstallPrice =
		ResourceBundle.Of(ResourceId.ScrapAlloy, 50);

	private static readonly ResourceBundle RailgunInstallPrice =
		ResourceBundle.Of(ResourceId.ScrapAlloy, 80);

	private static readonly ResourceBundle DefaultInstallPrice =
		ResourceBundle.Of(ResourceId.ScrapAlloy, 100);

	private const int DamageUpgradeBaseScrap = 40;
	private const int DamageUpgradeStepScrap = 20;
	private const int RangeUpgradeBaseScrap = 40;
	private const int RangeUpgradeStepScrap = 20;

	public static IReadOnlyList<MerchantCatalog.Offer> ListFor(ShipInstance ship)
	{
		ArgumentNullException.ThrowIfNull(ship);
		var offers = new List<MerchantCatalog.Offer>();

		foreach (var kind in SellableKinds)
		{
			var spec = ShipCatalog.DefaultAbilitySpec(EType.Fighter, kind);
			if (spec is null)
				continue;

			foreach (var facet in spec.CompatibleFacets)
			{
				var mount = new AbilityMount(kind, facet);
				if (ship.Spec.InstalledAbilities.Any(installed => installed.Mount == mount))
					continue;

				var offering = new MerchantCatalog.Offering(MerchantCatalog.Kind.InstallWeapon, mount);
				if (!MerchantShipChanges.TryPrepareAfter(offering, ship, out _))
					continue;

				var cost = mount.Kind switch
				{
					EAbilityKind.Flak => FlakInstallPrice,
					EAbilityKind.Railgun => RailgunInstallPrice,
					_ => DefaultInstallPrice,
				};
				offers.Add(new MerchantCatalog.Offer(offering, cost));
			}
		}

		foreach (var installed in ship.Spec.InstalledAbilities)
		{
			var damageOffering = new MerchantCatalog.Offering(
				MerchantCatalog.Kind.UpgradeDamage,
				installed.Mount);
			if (MerchantShipChanges.TryPrepareAfter(damageOffering, ship, out _))
			{
				offers.Add(new MerchantCatalog.Offer(
					damageOffering,
					ResourceBundle.Of(
						ResourceId.ScrapAlloy,
						DamageUpgradeBaseScrap + DamageUpgradeStepScrap * installed.Spec.DamageUpgradeTier)));
			}

			var rangeOffering = new MerchantCatalog.Offering(
				MerchantCatalog.Kind.UpgradeRange,
				installed.Mount);
			if (MerchantShipChanges.TryPrepareAfter(rangeOffering, ship, out _))
			{
				offers.Add(new MerchantCatalog.Offer(
					rangeOffering,
					ResourceBundle.Of(
						ResourceId.ScrapAlloy,
						RangeUpgradeBaseScrap + RangeUpgradeStepScrap * installed.Spec.RangeUpgradeTier)));
			}
		}

		return offers;
	}
}
