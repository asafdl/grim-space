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
	public void TryQuoteHullRepair_ChargesFixedCreditsAndScrap()
	{
		var ship = ShipInstance.FromCatalog("test-fighter", EType.Fighter);
		ship.HullPoints = 1;

		Assert.True(ShipSupportCatalog.TryQuoteHullRepair(ship, out var cost));
		Assert.True(cost.TryGet(ResourceId.Credits, out var credits));
		Assert.True(cost.TryGet(ResourceId.ScrapAlloy, out var scrap));
		Assert.Equal(ShipSupportCatalog.HullRepairCreditCost, credits);
		Assert.Equal(ShipSupportCatalog.HullRepairScrapCost, scrap);
	}

	[Fact]
	public void TryQuoteHullRepair_FailsWhenHullIsFull()
	{
		var ship = ShipInstance.FromCatalog("test-fighter", EType.Fighter);
		Assert.False(ShipSupportCatalog.TryQuoteHullRepair(ship, out _));
	}

	[Fact]
	public void TryQuoteShieldRecharge_ChargesTenCreditsPerMissingPoint()
	{
		var ship = ShipInstance.FromCatalog("test-fighter", EType.Fighter);
		var max = ship.TotalMaxShieldPoints;
		ship.ShieldPoints.Fill(0);

		Assert.True(ShipSupportCatalog.TryQuoteShieldRecharge(ship, out var cost));
		Assert.True(cost.TryGet(ResourceId.Credits, out var credits));
		Assert.Equal(max * ShipSupportCatalog.ShieldRechargeCreditsPerPoint, credits);
	}

	[Fact]
	public void TryQuoteShieldRecharge_FailsWhenAlreadyFull()
	{
		var ship = ShipInstance.FromCatalog("test-fighter", EType.Fighter);
		Assert.False(ShipSupportCatalog.TryQuoteShieldRecharge(ship, out _));
	}

	[Fact]
	public void TryQuoteShieldRechargeFace_QuotesOnlyRequestedFace()
	{
		var ship = ShipInstance.FromCatalog("test-fighter", EType.Fighter);
		ship.ShieldPoints[ESpatialOrientation.Forward] = 0;
		ship.ShieldPoints[ESpatialOrientation.Port] = 0;

		Assert.True(ShipSupportCatalog.TryQuoteShieldRechargeFace(
			ship,
			ESpatialOrientation.Forward,
			out var cost));
		Assert.True(cost.TryGet(ResourceId.Credits, out var credits));
		Assert.Equal(
			ship.MissingShieldPointsOnFace(ESpatialOrientation.Forward)
				* ShipSupportCatalog.ShieldRechargeCreditsPerPoint,
			credits);
	}
}
