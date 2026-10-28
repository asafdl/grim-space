using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Presentation.Facilities;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

[StarSystemTestSuite]
public sealed class MerchantOfferDisplayTests
{
	[Fact]
	public void ServiceBodies_ShowResultingState()
	{
		Assert.Equal("Hull integrity 4 \u2192 8", MerchantOfferDisplay.HullRepairBody(4, 8));
		Assert.Equal("Shield charge 1 \u2192 3", MerchantOfferDisplay.ShieldRechargeBody(1, 3));
		Assert.Equal("Total shields 10 \u2192 18", MerchantOfferDisplay.RechargeAllBody(10, 18));
	}
}
