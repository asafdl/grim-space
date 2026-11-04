using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Run.Persistence;

public sealed record ShipPersistenceDto(
	string Id,
	EType Chassis,
	int CurrentHullPoints,
	FaceShieldDto CurrentShieldPoints,
	int BaseHullPoints,
	FaceShieldDto BaseShieldPoints,
	FaceShieldDto ShieldUpgradeTiers,
	int HullUpgradeTier,
	IReadOnlyList<InstalledAbilityDto> InstalledAbilities,
	string? OwnerId,
	IReadOnlyList<EChassisBuffOffer> ChassisBuffs);
