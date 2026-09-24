using GrimSpace.Units;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Merchants;

public sealed record MerchantShipPurchase(
	string ShipId,
	ShipInstance Before,
	ShipInstance After,
	ResourceBundle Cost,
	string? OfferId = null);
