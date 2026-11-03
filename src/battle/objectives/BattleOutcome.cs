using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Battle.Objectives;

public sealed record UnitStateHandoff(
	string Id,
	EType Chassis,
	int CurrentHullPoints,
	FaceShieldPoints CurrentShieldPoints,
	EShipGearTier GearTier = EShipGearTier.T0);

public sealed record BattleOutcome(
	string BattleId,
	EBattleResult Result,
	IReadOnlyList<UnitStateHandoff> StateHandoffs,
	IReadOnlyDictionary<string, int> PerformancePointsByUnitId = null!)
{
	public static IReadOnlyDictionary<string, int> EmptyPerformancePoints { get; } =
		new Dictionary<string, int>(StringComparer.Ordinal);

	public BattleOutcome(
		string BattleId,
		EBattleResult Result,
		IReadOnlyList<UnitStateHandoff> StateHandoffs)
		: this(BattleId, Result, StateHandoffs, EmptyPerformancePoints)
	{
	}
}
