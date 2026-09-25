using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

[StarSystemTestSuite]
public sealed class ShipSupportCatalogTests
{
	[Fact]
	public void ListFor_HullRepair_ChargesFixedCreditsAndScrap()
	{
		var ship = ShipInstance.FromCatalog("test-fighter", EType.Fighter);
		ship.HullPoints = 1;

		var offer = ShipSupportCatalog.ListFor(ship)
			.Single(o => o.Offering.Kind == MerchantCatalog.Kind.RepairHull);
		Assert.True(offer.Cost.TryGet(ResourceId.Credits, out var credits));
		Assert.True(offer.Cost.TryGet(ResourceId.ScrapAlloy, out var scrap));
		Assert.Equal(ShipSupportCatalog.HullRepairCreditCost, credits);
		Assert.Equal(ShipSupportCatalog.HullRepairScrapCost, scrap);
	}

	[Fact]
	public void ListFor_ExcludesHullRepairWhenHullIsFull()
	{
		var ship = ShipInstance.FromCatalog("test-fighter", EType.Fighter);
		Assert.DoesNotContain(
			ShipSupportCatalog.ListFor(ship),
			offer => offer.Offering.Kind == MerchantCatalog.Kind.RepairHull);
	}

	[Fact]
	public void ListFor_ShieldRechargeAll_ChargesTenCreditsPerMissingPoint()
	{
		var ship = ShipInstance.FromCatalog("test-fighter", EType.Fighter);
		var max = ship.TotalMaxShieldPoints;
		ship.ShieldPoints.Fill(0);

		var offer = ShipSupportCatalog.ListFor(ship)
			.Single(o => o.Offering.Kind == MerchantCatalog.Kind.RechargeAllShields);
		Assert.True(offer.Cost.TryGet(ResourceId.Credits, out var credits));
		Assert.Equal(max * ShipSupportCatalog.ShieldRechargeCreditsPerPoint, credits);
	}

	[Fact]
	public void ListFor_ExcludesShieldRechargeAllWhenAlreadyFull()
	{
		var ship = ShipInstance.FromCatalog("test-fighter", EType.Fighter);
		Assert.DoesNotContain(
			ShipSupportCatalog.ListFor(ship),
			offer => offer.Offering.Kind == MerchantCatalog.Kind.RechargeAllShields);
	}

	[Fact]
	public void ListFor_ShieldRechargeFace_QuotesOnlyRequestedFace()
	{
		var ship = ShipInstance.FromCatalog("test-fighter", EType.Fighter);
		ship.ShieldPoints[ESpatialOrientation.Forward] = 0;
		ship.ShieldPoints[ESpatialOrientation.Port] = 0;

		var offer = ShipSupportCatalog.ListFor(ship)
			.Single(o => o.Offering == MerchantPurchaseTestHarness.RechargeShieldFace(ESpatialOrientation.Forward));
		Assert.True(offer.Cost.TryGet(ResourceId.Credits, out var credits));
		Assert.Equal(
			ship.MissingShieldPointsOnFace(ESpatialOrientation.Forward)
				* ShipSupportCatalog.ShieldRechargeCreditsPerPoint,
			credits);
	}

	[Fact]
	public void ListFor_IncludesShieldAndHullUpgradesAtTierZero()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var offers = ShipSupportCatalog.ListFor(ship);

		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
			Assert.Contains(offers, o => o.Offering == new MerchantCatalog.Offering(MerchantCatalog.Kind.UpgradeMaxShields, Face: face));
		Assert.Contains(offers, o => o.Offering.Kind == MerchantCatalog.Kind.UpgradeMaxHull);
	}

	[Fact]
	public void ListFor_ShieldUpgradeTierZeroCostsThirtyFiveScrap()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var offer = ShipSupportCatalog.ListFor(ship)
			.Single(o => o.Offering == new MerchantCatalog.Offering(MerchantCatalog.Kind.UpgradeMaxShields, Face: ESpatialOrientation.Forward));
		Assert.True(offer.Cost.TryGet(ResourceId.ScrapAlloy, out var scrap));
		Assert.Equal(35, scrap);
	}

	[Fact]
	public void ListFor_HullUpgradeTierZeroCostsTwentyFiveCreditsAndThirtyFiveScrap()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var offer = ShipSupportCatalog.ListFor(ship)
			.Single(o => o.Offering.Kind == MerchantCatalog.Kind.UpgradeMaxHull);
		Assert.True(offer.Cost.TryGet(ResourceId.Credits, out var credits));
		Assert.True(offer.Cost.TryGet(ResourceId.ScrapAlloy, out var scrap));
		Assert.Equal(25, credits);
		Assert.Equal(35, scrap);
	}

	[Fact]
	public void ListFor_PerFaceTierPricingAndCap_KeepOtherFacesAvailable()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		for (var tier = 0; tier < ShipLoadout.MaxShieldUpgradeTier; tier++)
		{
			var offer = ShipSupportCatalog.ListFor(ship).Single(o =>
				o.Offering == new MerchantCatalog.Offering(MerchantCatalog.Kind.UpgradeMaxShields, Face: ESpatialOrientation.Dorsal));
			Assert.True(offer.Cost.TryGet(ResourceId.ScrapAlloy, out var scrap));
			Assert.Equal(35 + 15 * tier, scrap);
			Assert.True(ship.TryWithUpgradedMaxShields(ESpatialOrientation.Dorsal, out ship));
		}

		Assert.DoesNotContain(
			ShipSupportCatalog.ListFor(ship),
			offer => offer.Offering == new MerchantCatalog.Offering(MerchantCatalog.Kind.UpgradeMaxShields, Face: ESpatialOrientation.Dorsal));
		Assert.Contains(
			ShipSupportCatalog.ListFor(ship),
			offer => offer.Offering == new MerchantCatalog.Offering(MerchantCatalog.Kind.UpgradeMaxShields, Face: ESpatialOrientation.Forward));
	}

	[Fact]
	public void ListFor_OffersUpgradeOnZeroMaxFace()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		ship.Loadout.MaxShieldPoints[ESpatialOrientation.Dorsal] = 0;
		ship.ShieldPoints[ESpatialOrientation.Dorsal] = 0;

		Assert.Contains(ShipSupportCatalog.ListFor(ship), offer =>
			offer.Offering == new MerchantCatalog.Offering(
				MerchantCatalog.Kind.UpgradeMaxShields, Face: ESpatialOrientation.Dorsal));
	}
}
