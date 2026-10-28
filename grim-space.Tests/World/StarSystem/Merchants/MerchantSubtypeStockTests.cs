using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

[StarSystemTestSuite]
public sealed class MerchantSubtypeStockTests(StarMapFixture maps)
{
	[Fact]
	public void HasOffers_ShipsAndPilotsHaveStockOnFreshMap()
	{
		var map = maps.Fresh(42);

		Assert.True(MerchantSubtypeStock.HasOffers(map, EMerchantSubtype.Ships));
		Assert.True(MerchantSubtypeStock.HasOffers(map, EMerchantSubtype.Pilots));
	}

	[Fact]
	public void HasOffers_ShipsListEveryRecruitmentTier()
	{
		var map = maps.Fresh(42);

		Assert.Equal(6, map.Market.ListRecruitmentOffers().Count);
		Assert.True(MerchantSubtypeStock.HasOffers(map, EMerchantSubtype.Ships));
	}
}
