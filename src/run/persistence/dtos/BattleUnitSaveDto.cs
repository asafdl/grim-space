using GrimSpace.Battle;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.World.Factions;

namespace GrimSpace.Run.Persistence;

public sealed record BattleUnitSaveDto(
	ShipPersistenceDto Ship,
	ETeam Team,
	EFaction Faction,
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
	int CurrentManeuverPoints,
	int BaseMp,
	int Initiative,
	IReadOnlyList<BattleStatusDto> Statuses,
	bool ShieldsUpUsedThisTurn,
	int OverchargeEnginesCooldownRemaining,
	int MarkWeakspotCooldownRemaining,
	bool ShieldBraceUsedThisTurn,
	bool HeatVentsUsedThisTurn,
	int DefensiveManeuversMpUsed,
	int ShieldDynamoGrantedPerFace,
	int ReactiveShieldsStacks,
	int Heat,
	int? HeatCapacity,
	int HeatClearPerTurn,
	float HeatGainMultiplier,
	bool Overdrive,
	bool Overheat,
	int ThirstForScrapStacks,
	int? HullHandoffBasePoints = null,
	FaceShieldDto? ShieldHandoffBasePoints = null);
