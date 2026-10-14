namespace GrimSpace.World.StarSystem.Merchants;

public sealed record MerchantCatalogQuote(
	MerchantEndpoint Endpoint,
	EMerchantCatalog Catalog,
	string ShipId,
	IReadOnlyList<MerchantCatalog.Offer> Offers);
