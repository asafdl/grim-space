using GrimSpace.Units.Enums;

namespace GrimSpace.Units;

public sealed record ShipSpawnDeclaration(
	string ShipId,
	EType Chassis,
	EShipGearTier GearTier = EShipGearTier.T0,
	int? RollSeed = null);
