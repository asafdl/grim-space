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
	public void T0_ListsOneGenericShieldPackWithEligibleFaces()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var offer = MerchantCatalog.ListFor(
				EMerchantCatalog.ShipSupport,
				ship,
				EMerchantAvailabilityBand.T0)
			.Single(candidate => candidate.Offering.Kind == MerchantCatalog.Kind.UpgradeMaxShields);

		Assert.Equal(1, offer.Offering.TargetLevel);
		Assert.DoesNotContain(
			new MerchantCatalog.Target(Face: ESpatialOrientation.Forward),
			offer.Targets);
		Assert.Contains(
			new MerchantCatalog.Target(Face: ESpatialOrientation.Retro),
			offer.Targets);
	}

	[Fact]
	public void T0_5_IncludesT0ShieldPack()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);

		Assert.Contains(
			MerchantCatalog.ListFor(
				EMerchantCatalog.ShipSupport,
				ship,
				EMerchantAvailabilityBand.T0_5),
			offer => offer.Offering == new MerchantCatalog.Offering(
				MerchantCatalog.Kind.UpgradeMaxShields,
				1));
	}

	[Fact]
	public void T1_DoesNotIncludeT0ShieldPack()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);

		Assert.DoesNotContain(
			MerchantCatalog.ListFor(
				EMerchantCatalog.ShipSupport,
				ship,
				EMerchantAvailabilityBand.T1),
			offer => offer.Offering == new MerchantCatalog.Offering(
				MerchantCatalog.Kind.UpgradeMaxShields,
				1));
	}

	[Fact]
	public void ShieldPack_ExcludesFacesAlreadyAtTargetMaximum()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var face = ESpatialOrientation.Retro;
		Assert.True(ship.TryWithMaxShieldPoints(face, (1 * 100), out ship));

		var offer = MerchantCatalog.ListFor(
				EMerchantCatalog.ShipSupport,
				ship,
				EMerchantAvailabilityBand.T0)
			.Single(candidate => candidate.Offering.Kind == MerchantCatalog.Kind.UpgradeMaxShields);

		Assert.DoesNotContain(new MerchantCatalog.Target(Face: face), offer.Targets);
	}

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
		Assert.Equal([default(MerchantCatalog.Target)], repair.Targets);
		Assert.True(repair.Cost.TryGet(ResourceId.Credits, out _));

		var recharge = offers.Single(offer =>
			offer.Offering.Kind == MerchantCatalog.Kind.RechargeAllShields);
		Assert.Equal([default(MerchantCatalog.Target)], recharge.Targets);
		Assert.True(recharge.Cost.TryGet(ResourceId.Credits, out var credits));
		Assert.Equal(
			ship.TotalMaxShieldPoints * StarMarketSeed.ShieldRechargeCreditsPerPoint / 100,
			credits);

		var faceRecharge = offers.Single(offer =>
			offer.Offering.Kind == MerchantCatalog.Kind.RechargeShieldFace
				&& offer.Targets.Contains(new MerchantCatalog.Target(
					Face: ESpatialOrientation.Forward)));
		Assert.Single(faceRecharge.Targets);
	}
}
