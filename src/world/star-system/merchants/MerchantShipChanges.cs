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
				if (target != default)
					return false;
				if (next.MissingHullPoints <= 0)
					return false;
				next.CurrentHullPoints = next.Loadout.BaseHullPoints;
				break;

			case MerchantCatalog.Kind.RechargeAllShields:
				if (target != default)
					return false;
				if (next.MissingShieldPoints <= 0)
					return false;
				foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
					next.CurrentShieldPoints[face] = next.Loadout.BaseShieldPoints[face];
				break;

			case MerchantCatalog.Kind.RechargeShieldFace:
				if (target.Face is not { } rechargeFace)
					return false;
				if (next.MissingShieldPointsOnFace(rechargeFace) <= 0)
					return false;
				next.CurrentShieldPoints[rechargeFace] = next.Loadout.BaseShieldPoints[rechargeFace];
				break;

			case MerchantCatalog.Kind.BuyFuel:
				if (target != default)
					return false;
				break;

			default:
				return false;
		}

		after = next;
		return true;
	}
}
