using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

[StarSystemTestSuite]
public sealed class HealingCatalogTests
{
	[Fact]
	public void HealingCatalog_ListsRepairAndRechargeServices()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		ship.HullPoints = (1 * 100);
		ship.ShieldPoints.Fill(0);

		var offers = MerchantCatalog.ListFor(
			EMerchantCatalog.Healing,
			ship,
			EMerchantAvailabilityBand.T2);

		var repair = offers.Single(offer => offer.Offering.Kind == MerchantCatalog.Kind.RepairHull);
		Assert.Equal(default, repair.Target);
		Assert.True(repair.Cost.TryGet(ResourceId.Credits, out _));

		var buyFuel = offers.Single(offer => offer.Offering.Kind == MerchantCatalog.Kind.BuyFuel);
		Assert.Equal(default, buyFuel.Target);
		Assert.True(buyFuel.Cost.TryGet(ResourceId.Credits, out var fuelCredits));
		Assert.Equal(StarMarketSeed.FuelPurchaseCreditCost, fuelCredits);

		var recharge = offers.Single(offer =>
			offer.Offering.Kind == MerchantCatalog.Kind.RechargeAllShields);
		Assert.Equal(default, recharge.Target);
		Assert.True(recharge.Cost.TryGet(ResourceId.Credits, out var credits));
		Assert.Equal(
			ship.TotalMaxShieldPoints * StarMarketSeed.ShieldRechargeCreditsPerPoint / 100,
			credits);

		Assert.Single(offers, offer =>
			offer.Offering.Kind == MerchantCatalog.Kind.RechargeShieldFace
				&& offer.Target.Face == ESpatialOrientation.Forward);
	}
}
