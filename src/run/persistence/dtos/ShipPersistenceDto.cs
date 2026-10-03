using GrimSpace.Units.Enums;

namespace GrimSpace.Run.Persistence;

public sealed record ShipPersistenceDto(
	string Id,
	EType Chassis,
	int HullPoints,
	FaceShieldDto ShieldPoints,
	int MaxHullPoints,
	FaceShieldDto MaxShieldPoints,
	FaceShieldDto ShieldUpgradeTiers,
	int HullUpgradeTier,
	IReadOnlyList<InstalledAbilityDto> InstalledAbilities);
