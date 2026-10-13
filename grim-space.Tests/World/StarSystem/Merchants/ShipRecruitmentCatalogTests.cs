using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

[StarSystemTestSuite]
public sealed class ShipRecruitmentCatalogTests
{
	private static StarMarket Market => StarMarketSeed.Create();

	[Fact]
	public void T0_HasNoRecruitmentOffers()
	{
		Assert.Empty(Market.ListRecruitmentOffers(EMerchantAvailabilityBand.T0));
	}

	[Fact]
	public void T1_OffersRepurposedMiner()
	{
		var offer = Assert.Single(Market.ListRecruitmentOffers(EMerchantAvailabilityBand.T1));

		Assert.Equal(EType.RepurposedMiner, offer.Chassis);
	}

	[Fact]
	public void T1_5_IncludesT1RepurposedMiner()
	{
		var offer = Assert.Single(Market.ListRecruitmentOffers(EMerchantAvailabilityBand.T1_5));

		Assert.Equal(EType.RepurposedMiner, offer.Chassis);
	}

	[Fact]
	public void T2_OffersGunshipOnly()
	{
		var offer = Assert.Single(Market.ListRecruitmentOffers(EMerchantAvailabilityBand.T2));

		Assert.Equal(EType.Gunship, offer.Chassis);
	}
}
