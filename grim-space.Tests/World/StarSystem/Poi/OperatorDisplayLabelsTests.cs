using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;

namespace GrimSpace.Tests.World.StarSystem.Poi;

[StarSystemTestSuite]
public sealed class OperatorDisplayLabelsTests
{
	[Theory]
	[InlineData(EMerchantCatalog.Weapons, "Dockyard Shop")]
	[InlineData(EMerchantCatalog.ShipSupport, "Ship Support")]
	[InlineData(EMerchantCatalog.Ships, "Ship Broker")]
	public void MerchantRole_LabelsEveryCatalog(EMerchantCatalog catalog, string expected)
	{
		Assert.Equal(expected, OperatorDisplayLabels.MerchantRole(catalog));
	}
}
