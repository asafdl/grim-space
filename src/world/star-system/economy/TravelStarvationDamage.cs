using GrimSpace.Units;

namespace GrimSpace.World.StarSystem.Economy;

public sealed record TravelStarvationDamage(
	IReadOnlyList<TravelStarvationShipDamage> Ships);

public sealed record TravelStarvationShipDamage(
	string ShipId,
	ShipInstance Before,
	ShipInstance After);
