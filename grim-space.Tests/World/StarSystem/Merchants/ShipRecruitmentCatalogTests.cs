using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

[StarSystemTestSuite]
public sealed class ShipRecruitmentCatalogTests
{
	[Fact]
	public void T0_HasNoRecruitmentOffers()
	{
		Assert.Empty(ShipRecruitmentCatalog.ListFor(EMerchantAvailabilityBand.T0));
	}

	[Fact]
	public void T1_OffersRepurposedMiner()
	{
		var offer = Assert.Single(ShipRecruitmentCatalog.ListFor(EMerchantAvailabilityBand.T1));

		Assert.Equal(EType.RepurposedMiner, offer.Chassis);
	}

	[Fact]
	public void T1_5_IncludesT1RepurposedMiner()
	{
		var offer = Assert.Single(ShipRecruitmentCatalog.ListFor(EMerchantAvailabilityBand.T1_5));

		Assert.Equal(EType.RepurposedMiner, offer.Chassis);
	}

	[Fact]
	public void T2_OffersGunshipOnly()
	{
		var offer = Assert.Single(ShipRecruitmentCatalog.ListFor(EMerchantAvailabilityBand.T2));

		Assert.Equal(EType.Gunship, offer.Chassis);
	}
}
