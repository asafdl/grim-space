using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Merchants;

internal static class ShipSupportCatalog
{
	public const int HullRepairCreditCost = 20;
	public const int HullRepairScrapCost = 20;
	public const int ShieldRechargeCreditsPerPoint = 10;

	public static IReadOnlyList<MerchantCatalog.Offer> ListServicesFor(ShipInstance ship)
	{
		ArgumentNullException.ThrowIfNull(ship);
		var offers = new List<MerchantCatalog.Offer>();

		var repair = new MerchantCatalog.Offering(MerchantCatalog.Kind.RepairHull);
		if (MerchantShipChanges.TryPrepareAfter(repair, default, ship, out _))
		{
			offers.Add(new MerchantCatalog.Offer(
				repair,
				ResourceBundle.Create(
					(ResourceId.Credits, HullRepairCreditCost),
					(ResourceId.ScrapAlloy, HullRepairScrapCost)),
				[default]));
		}

		var rechargeAll = new MerchantCatalog.Offering(MerchantCatalog.Kind.RechargeAllShields);
		if (MerchantShipChanges.TryPrepareAfter(rechargeAll, default, ship, out _))
		{
			offers.Add(new MerchantCatalog.Offer(
				rechargeAll,
				ResourceBundle.Of(
					ResourceId.Credits,
					ship.MissingShieldPoints * ShieldRechargeCreditsPerPoint),
				[default]));
		}

		foreach (var face in Enum.GetValues<ESpatialOrientation>())
		{
			var target = new MerchantCatalog.Target(Face: face);
			var rechargeFace = new MerchantCatalog.Offering(MerchantCatalog.Kind.RechargeShieldFace);
			if (!MerchantShipChanges.TryPrepareAfter(rechargeFace, target, ship, out _))
				continue;

			offers.Add(new MerchantCatalog.Offer(
				rechargeFace,
				ResourceBundle.Of(
					ResourceId.Credits,
					ship.MissingShieldPointsOnFace(face) * ShieldRechargeCreditsPerPoint),
				[target]));
		}

		return offers;
	}
}
