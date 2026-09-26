using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Merchants;

public static class ShipSupportCatalog
{
	public const int HullRepairCreditCost = 20;
	public const int HullRepairScrapCost = 20;
	public const int ShieldRechargeCreditsPerPoint = 10;

	public static IReadOnlyList<MerchantCatalog.Offer> ListFor(ShipInstance ship)
	{
		ArgumentNullException.ThrowIfNull(ship);
		var offers = new List<MerchantCatalog.Offer>();

		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
		{
			var maxShields = new MerchantCatalog.Offering(MerchantCatalog.Kind.UpgradeMaxShields, Face: face);
			if (!MerchantShipChanges.TryPrepareAfter(maxShields, ship, out _))
				continue;
			offers.Add(new MerchantCatalog.Offer(
				maxShields,
				MerchantUpgradePricing.ShieldMaxUpgrade(ship.Loadout.ShieldUpgradeTiers[face])));
		}

		var maxHull = new MerchantCatalog.Offering(MerchantCatalog.Kind.UpgradeMaxHull);
		if (MerchantShipChanges.TryPrepareAfter(maxHull, ship, out _))
		{
			offers.Add(new MerchantCatalog.Offer(
				maxHull,
				MerchantUpgradePricing.HullMaxUpgrade(ship.Loadout.HullUpgradeTier)));
		}

		var repair = new MerchantCatalog.Offering(MerchantCatalog.Kind.RepairHull);
		if (MerchantShipChanges.TryPrepareAfter(repair, ship, out _))
		{
			offers.Add(new MerchantCatalog.Offer(
				repair,
				ResourceBundle.Create(
					(ResourceId.Credits, HullRepairCreditCost),
					(ResourceId.ScrapAlloy, HullRepairScrapCost))));
		}

		var rechargeAll = new MerchantCatalog.Offering(MerchantCatalog.Kind.RechargeAllShields);
		if (MerchantShipChanges.TryPrepareAfter(rechargeAll, ship, out _))
		{
			offers.Add(new MerchantCatalog.Offer(
				rechargeAll,
				ResourceBundle.Of(
					ResourceId.Credits,
					ship.MissingShieldPoints * ShieldRechargeCreditsPerPoint)));
		}

		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
		{
			if (ship.Loadout.MaxShieldPoints[face] <= 0)
				continue;

			var faceOffering = new MerchantCatalog.Offering(
				MerchantCatalog.Kind.RechargeShieldFace,
				Face: face);
			if (!MerchantShipChanges.TryPrepareAfter(faceOffering, ship, out _))
				continue;

			offers.Add(new MerchantCatalog.Offer(
				faceOffering,
				ResourceBundle.Of(
					ResourceId.Credits,
					ship.MissingShieldPointsOnFace(face) * ShieldRechargeCreditsPerPoint)));
		}

		return offers;
	}
}
