using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Merchants;

public static class ShipSupportCatalog
{
	public const int HullRepairCreditCost = 20;
	public const int HullRepairScrapCost = 20;
	public const int ShieldRechargeCreditsPerPoint = 10;

	private const int ShieldUpgradeBaseScrap = 35;
	private const int ShieldUpgradeStepScrap = 15;
	private const int HullUpgradeCreditCost = 25;
	private const int HullUpgradeBaseScrap = 35;
	private const int HullUpgradeStepScrap = 15;

	public static IReadOnlyList<MerchantCatalog.Offer> ListFor(ShipInstance ship)
	{
		ArgumentNullException.ThrowIfNull(ship);
		var offers = new List<MerchantCatalog.Offer>();

		var maxShields = new MerchantCatalog.Offering(MerchantCatalog.Kind.UpgradeMaxShields);
		if (MerchantShipChanges.TryPrepareAfter(maxShields, ship, out _))
		{
			offers.Add(new MerchantCatalog.Offer(
				maxShields,
				ResourceBundle.Of(
					ResourceId.ScrapAlloy,
					ShieldUpgradeBaseScrap + ShieldUpgradeStepScrap * ship.Spec.ShieldUpgradeTier)));
		}

		var maxHull = new MerchantCatalog.Offering(MerchantCatalog.Kind.UpgradeMaxHull);
		if (MerchantShipChanges.TryPrepareAfter(maxHull, ship, out _))
		{
			offers.Add(new MerchantCatalog.Offer(
				maxHull,
				ResourceBundle.Create(
					(ResourceId.Credits, HullUpgradeCreditCost),
					(ResourceId.ScrapAlloy, HullUpgradeBaseScrap + HullUpgradeStepScrap * ship.Spec.HullUpgradeTier))));
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
			if (ship.Spec.MaxShieldPoints[face] <= 0)
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
