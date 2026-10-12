using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

[StarSystemTestSuite]
public sealed class MerchantSubtypeStockTests(StarMapFixture maps)
{
	[Fact]
	public void HasOffers_AtT0_OnlyShipSupportHasStock()
	{
		var map = maps.Fresh(42);

		Assert.False(MerchantSubtypeStock.HasOffers(map, EMerchantSubtype.Weapons));
		Assert.True(MerchantSubtypeStock.HasOffers(map, EMerchantSubtype.ShipSupport));
		Assert.False(MerchantSubtypeStock.HasOffers(map, EMerchantSubtype.Ships));
	}

	[Fact]
	public void HasOffers_AtT1_AllSubtypesHaveStock()
	{
		var map = maps.Fresh(42);
		MerchantCatalogTestBands.SetBand(map, EMerchantAvailabilityBand.T1);

		Assert.True(MerchantSubtypeStock.HasOffers(map, EMerchantSubtype.Weapons));
		Assert.True(MerchantSubtypeStock.HasOffers(map, EMerchantSubtype.ShipSupport));
		Assert.True(MerchantSubtypeStock.HasOffers(map, EMerchantSubtype.Ships));
	}
}
