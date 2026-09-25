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
				next.HullPoints = next.Loadout.MaxHullPoints;
				break;

			case MerchantCatalog.Kind.UpgradeMaxShields:
				if (key.Face is not { } upgradeFace || !next.TryWithUpgradedMaxShields(upgradeFace, out var upgraded))
					return false;
				next = upgraded;
				break;

			case MerchantCatalog.Kind.UpgradeMaxHull:
				if (next.Loadout.HullUpgradeTier >= ShipLoadout.MaxHullUpgradeTier)
					return false;
				if (!next.TryWithUpgradedMaxHull(out upgraded))
					return false;
				next = upgraded;
				break;

			case MerchantCatalog.Kind.RechargeAllShields:
				if (next.MissingShieldPoints <= 0)
					return false;
				foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
					next.ShieldPoints[face] = next.Loadout.MaxShieldPoints[face];
				break;

			case MerchantCatalog.Kind.RechargeShieldFace:
				if (key.Face is not { } rechargeFace)
					return false;
				if (next.MissingShieldPointsOnFace(rechargeFace) <= 0)
					return false;
				next.ShieldPoints[rechargeFace] = next.Loadout.MaxShieldPoints[rechargeFace];
				break;

			case MerchantCatalog.Kind.InstallWeapon:
				if (key.Mount is not { } installMount)
					return false;
				if (next.Loadout.InstalledAbilities.Any(installed => installed.Mount == installMount))
					return false;
				if (!next.Spec.TryGetBaseline(installMount, out var installSpec) || installSpec is null)
					return false;
				if (!installSpec.CompatibleFacets.Contains(installMount.Facet))
					return false;
				if (!next.TryWithInstalledAbility(new InstalledAbility(installSpec, installMount.Facet), out upgraded))
					return false;
				next = upgraded;
				break;

			case MerchantCatalog.Kind.UpgradeDamage:
				if (key.Mount is not { } damageMount)
					return false;
				if (!next.TryWithDamageUpgraded(damageMount, out upgraded))
					return false;
				next = upgraded;
				break;

			case MerchantCatalog.Kind.UpgradeRange:
				if (key.Mount is not { } rangeMount)
					return false;
				if (!next.TryWithRangeUpgraded(rangeMount, out upgraded))
					return false;
				next = upgraded;
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
				key.Mount is null && key.Face is { } rechargeFace && Enum.IsDefined(rechargeFace),
			MerchantCatalog.Kind.UpgradeMaxShields =>
				key.Mount is null && key.Face is { } upgradeFace && Enum.IsDefined(upgradeFace),
			_ => key.Mount is null && key.Face is null,
		};
}
