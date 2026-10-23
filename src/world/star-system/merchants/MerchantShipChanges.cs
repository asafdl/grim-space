using GrimSpace.Math.Grid;
using GrimSpace.Units;
namespace GrimSpace.World.StarSystem.Merchants;

internal static class MerchantShipChanges
{
	internal static bool TryPrepareAfter(
		MerchantCatalog.Offering offering,
		MerchantCatalog.Target target,
		ShipInstance before,
		out ShipInstance after)
	{
		after = null!;
		var next = before.Clone();

		switch (offering.Kind)
		{
			case MerchantCatalog.Kind.RepairHull:
				if (target != default || offering.TargetLevel != 0)
					return false;
				if (next.MissingHullPoints <= 0)
					return false;
				next.HullPoints = next.Loadout.MaxHullPoints;
				break;

			case MerchantCatalog.Kind.UpgradeMaxShields:
				if (target.Face is not { } upgradeFace
					|| target.Mount is not null
					|| offering.TargetLevel <= 0)
					return false;
				if (!next.TryWithMaxShieldPoints(
						upgradeFace,
						offering.TargetLevel * 100,
						out var upgraded))
					return false;
				next = upgraded;
				break;

			case MerchantCatalog.Kind.RechargeAllShields:
				if (target != default || offering.TargetLevel != 0)
					return false;
				if (next.MissingShieldPoints <= 0)
					return false;
				foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
					next.ShieldPoints[face] = next.Loadout.MaxShieldPoints[face];
				break;

			case MerchantCatalog.Kind.RechargeShieldFace:
				if (target.Face is not { } rechargeFace
					|| target.Mount is not null
					|| offering.TargetLevel != 0)
					return false;
				if (next.MissingShieldPointsOnFace(rechargeFace) <= 0)
					return false;
				next.ShieldPoints[rechargeFace] = next.Loadout.MaxShieldPoints[rechargeFace];
				break;

			case MerchantCatalog.Kind.UpgradeDamage:
				if (target.Mount is not { } damageMount
					|| target.Face is not null
					|| offering.TargetLevel <= 0)
					return false;
				if (!next.TryWithDamageUpgradeTier(
						damageMount,
						offering.TargetLevel,
						out upgraded))
					return false;
				next = upgraded;
				break;

			case MerchantCatalog.Kind.UpgradeRange:
				if (target.Mount is not { } rangeMount
					|| target.Face is not null
					|| offering.TargetLevel <= 0)
					return false;
				if (!next.TryWithRangeUpgradeTier(
						rangeMount,
						offering.TargetLevel,
						out upgraded))
					return false;
				next = upgraded;
				break;

			default:
				return false;
		}

		after = next;
		return true;
	}
}
