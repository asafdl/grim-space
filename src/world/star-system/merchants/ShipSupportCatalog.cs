using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Merchants;

public static class ShipSupportCatalog
{
	public const int HullRepairCreditCost = 20;
	public const int HullRepairScrapCost = 20;
	public const int ShieldRechargeCreditsPerPoint = 10;

	public static bool TryQuoteHullRepair(ShipInstance ship, out ResourceBundle cost)
	{
		cost = ResourceBundle.Empty;
		if (ship.MissingHullPoints <= 0)
			return false;

		cost = ResourceBundle.Create(
			(ResourceId.Credits, HullRepairCreditCost),
			(ResourceId.ScrapAlloy, HullRepairScrapCost));
		return true;
	}

	public static bool TryQuoteShieldRechargeFace(
		ShipInstance ship,
		ESpatialOrientation face,
		out ResourceBundle cost)
	{
		cost = ResourceBundle.Empty;
		var missing = ship.MissingShieldPointsOnFace(face);
		if (missing <= 0)
			return false;

		cost = ResourceBundle.Of(ResourceId.Credits, missing * ShieldRechargeCreditsPerPoint);
		return true;
	}

	public static bool TryQuoteShieldRecharge(ShipInstance ship, out ResourceBundle cost)
	{
		cost = ResourceBundle.Empty;
		var missing = ship.MissingShieldPoints;
		if (missing <= 0)
			return false;

		cost = ResourceBundle.Of(ResourceId.Credits, missing * ShieldRechargeCreditsPerPoint);
		return true;
	}
}
