using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Battle.Objectives;

public sealed record UnitStateHandoff(
	string Id,
	EType Chassis,
	int HullPoints,
	FaceShieldPoints ShieldPoints);

public sealed record BattleOutcome(
	string BattleId,
	EBattleResult Result,
	IReadOnlyList<UnitStateHandoff> StateHandoffs);
