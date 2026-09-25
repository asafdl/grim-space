using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.World.StarSystem.Merchants;

internal static class MerchantShipChanges
{
	internal static bool TryPrepareAfter(
		MerchantCatalog.Offering key,
		ShipInstance before,
		out ShipInstance after)
	{
		after = null!;
		if (!IsWellFormed(key))
			return false;

		var next = before.Clone();

		switch (key.Kind)
		{
			case MerchantCatalog.Kind.RepairHull:
				if (next.MissingHullPoints <= 0)
					return false;
				next.HullPoints = next.Spec.MaxHullPoints;
				break;

			case MerchantCatalog.Kind.UpgradeMaxShields:
				if (next.Spec.ShieldUpgradeTier >= ShipSpec.MaxShieldUpgradeTier)
					return false;
				var oldMax = next.Spec.MaxShieldPoints;
				next.Spec = next.Spec.WithUpgradedMaxShields();
				foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
				{
					next.ShieldPoints[face] = System.Math.Min(
						next.ShieldPoints[face] + next.Spec.MaxShieldPoints[face] - oldMax[face],
						next.Spec.MaxShieldPoints[face]);
				}
				break;

			case MerchantCatalog.Kind.UpgradeMaxHull:
				if (next.Spec.HullUpgradeTier >= ShipSpec.MaxHullUpgradeTier)
					return false;
				next.Spec = next.Spec.WithUpgradedMaxHull();
				break;

			case MerchantCatalog.Kind.RechargeAllShields:
				if (next.MissingShieldPoints <= 0)
					return false;
				foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
					next.ShieldPoints[face] = next.Spec.MaxShieldPoints[face];
				break;

			case MerchantCatalog.Kind.RechargeShieldFace:
				if (key.Face is not { } rechargeFace)
					return false;
				if (next.MissingShieldPointsOnFace(rechargeFace) <= 0)
					return false;
				next.ShieldPoints[rechargeFace] = next.Spec.MaxShieldPoints[rechargeFace];
				break;

			case MerchantCatalog.Kind.InstallWeapon:
				if (key.Mount is not { } installMount)
					return false;
				if (next.Spec.InstalledAbilities.Any(installed => installed.Mount == installMount))
					return false;
				var installSpec = ShipCatalog.DefaultAbilitySpec(EType.Fighter, installMount.Kind);
				if (installSpec is null || !installSpec.CompatibleFacets.Contains(installMount.Facet))
					return false;
				try
				{
					next.Spec = next.Spec.WithInstalledAbility(
						new InstalledAbility(installSpec, installMount.Facet));
				}
				catch (ArgumentException)
				{
					return false;
				}
				break;

			case MerchantCatalog.Kind.UpgradeDamage:
				if (key.Mount is not { } damageMount)
					return false;
				var damageInstalled = next.Spec.InstalledAbilities.FirstOrDefault(a => a.Mount == damageMount);
				if (damageInstalled is null || !damageInstalled.Spec.TryCreateDamageUpgraded(out var damageReplacement))
					return false;
				next.Spec = next.Spec.WithReplacedMount(damageMount, damageReplacement);
				break;

			case MerchantCatalog.Kind.UpgradeRange:
				if (key.Mount is not { } rangeMount)
					return false;
				var rangeInstalled = next.Spec.InstalledAbilities.FirstOrDefault(a => a.Mount == rangeMount);
				if (rangeInstalled is null || !rangeInstalled.Spec.TryCreateRangeUpgraded(out var rangeReplacement))
					return false;
				next.Spec = next.Spec.WithReplacedMount(rangeMount, rangeReplacement);
				break;

			default:
				return false;
		}

		after = next;
		return true;
	}

	private static bool IsWellFormed(MerchantCatalog.Offering key) =>
		key.Kind switch
		{
			MerchantCatalog.Kind.InstallWeapon
				or MerchantCatalog.Kind.UpgradeDamage
				or MerchantCatalog.Kind.UpgradeRange =>
				key.Mount is not null && key.Face is null,
			MerchantCatalog.Kind.RechargeShieldFace =>
				key.Mount is null && key.Face is not null,
			_ => key.Mount is null && key.Face is null,
		};
}
