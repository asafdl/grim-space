using GrimSpace.Units;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Dockyard;

public sealed record HullRepairPurchased(
	string ShipId,
	ShipInstance Before,
	ShipInstance After,
	ResourceBundle Cost);
