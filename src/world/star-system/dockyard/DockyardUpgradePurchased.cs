using GrimSpace.Units;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Dockyard;

public sealed record DockyardUpgradePurchased(
	string ShipId,
	ShipInstance Before,
	ShipInstance After,
	string OfferId,
	ResourceBundle Cost);
