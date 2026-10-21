using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Presentation.Facilities;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

[StarSystemTestSuite]
public sealed class MerchantSubtypeRoutingTests
{
	[Fact]
	public void PresentationFor_RoutesSpecializedSheet()
	{
		Assert.Equal(
			EMerchantPresentation.Upgrades,
			MerchantSubtypeRouting.PresentationFor(EMerchantSubtype.Weapons));
		Assert.Equal(
			EMerchantPresentation.Upgrades,
			MerchantSubtypeRouting.PresentationFor(EMerchantSubtype.ShipSupport));
		Assert.Equal(
			EMerchantPresentation.Recruitment,
			MerchantSubtypeRouting.PresentationFor(EMerchantSubtype.Ships));
		Assert.Equal(
			EMerchantPresentation.PilotHiring,
			MerchantSubtypeRouting.PresentationFor(EMerchantSubtype.Pilots));
	}

	[Fact]
	public void CatalogFor_Pilots_Throws()
	{
		Assert.Throws<InvalidOperationException>(() =>
			MerchantSubtypeRouting.CatalogFor(EMerchantSubtype.Pilots));
	}

	[Theory]
	[InlineData(EMerchantSubtype.Weapons, EMerchantCatalog.Weapons)]
	[InlineData(EMerchantSubtype.ShipSupport, EMerchantCatalog.ShipSupport)]
	[InlineData(EMerchantSubtype.Ships, EMerchantCatalog.Ships)]
	public void CatalogFor_MapsLegacyPurchaseCatalog(
		EMerchantSubtype subtype,
		EMerchantCatalog expected)
	{
		Assert.Equal(expected, MerchantSubtypeRouting.CatalogFor(subtype));
	}
}
