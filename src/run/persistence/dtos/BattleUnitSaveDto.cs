using GrimSpace.Battle;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Run.Persistence;

public sealed record BattleUnitSaveDto(
	ShipPersistenceDto Ship,
	ETeam Team,
	EBattleAgentKind AgentKind,
	Coord Position,
	Coord Fore,
	Coord Dorsal,
	Coord Starboard,
	int ActionPoints,
	int FuelRemaining,
	string ParentId,
	int MaxAp,
	VoidBombProjectile? Projectile,
	IReadOnlyList<MountRuntimeDto> MountRuntime,
	int? ManeuverPoints = null,
	int? MaxMp = null,
	int? Initiative = null,
	IReadOnlyList<BattleStatusDto>? Statuses = null,
	bool ShieldsUpUsedThisTurn = false,
	int OverchargeEnginesCooldownRemaining = 0,
	int ProtectionDroneRollIndex = 0,
	bool ShieldBraceUsedThisTurn = false,
	bool HeatVentsUsedThisTurn = false,
	int DefensiveManeuversMpUsed = 0,
	int DefensiveManeuversRollIndex = 0,
	int ReactiveShieldsStacks = 0,
	int Heat = 0,
	int? HeatCapacity = null,
	int? HeatClearPerTurn = null,
	float? HeatGainMultiplier = null,
	bool Overdrive = false,
	bool Overheat = false);
