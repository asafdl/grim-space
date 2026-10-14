using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Run.Persistence;

internal sealed record PurchaseDto(
	string ActorId,
	MerchantEndpoint? Endpoint,
	string? PoiId,
	string? FacilityId,
	string? OperatorName,
	EMerchantCatalog Catalog,
	MerchantCatalog.Offering Offering,
	MerchantCatalog.Target Target,
	ShipPersistenceDto Before,
	ResourceBundle? QuotedCost = null);
