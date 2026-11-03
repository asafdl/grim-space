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
	int CurrentActionPoints,
	int FuelRemaining,
	string ParentId,
	int BaseAp,
	VoidBombProjectile? Projectile,
	IReadOnlyList<MountRuntimeDto> MountRuntime,
	int? CurrentManeuverPoints = null,
	int? BaseMp = null,
	int? Initiative = null,
	IReadOnlyList<BattleStatusDto>? Statuses = null,
	bool ShieldsUpUsedThisTurn = false,
	int OverchargeEnginesCooldownRemaining = 0,
	int MarkWeakspotCooldownRemaining = 0,
	bool ShieldBraceUsedThisTurn = false,
	bool HeatVentsUsedThisTurn = false,
	int DefensiveManeuversMpUsed = 0,
	int DefensiveManeuversRollIndex = 0,
	int ShieldDynamoGrantedPerFace = 0,
	int ReactiveShieldsStacks = 0,
	int Heat = 0,
	int? HeatCapacity = null,
	int? HeatClearPerTurn = null,
	float? HeatGainMultiplier = null,
	bool Overdrive = false,
	bool Overheat = false);
