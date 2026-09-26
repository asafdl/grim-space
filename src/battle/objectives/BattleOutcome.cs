using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Battle.Objectives;

public sealed record UnitStateHandoff(
	string Id,
	EType Chassis,
	int HullPoints,
	FaceShieldPoints ShieldPoints,
	EShipGearTier GearTier = EShipGearTier.T0);

public sealed record BattleOutcome(
	string BattleId,
	EBattleResult Result,
	IReadOnlyList<UnitStateHandoff> StateHandoffs);
