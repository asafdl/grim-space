using GrimSpace.Battle;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;

namespace GrimSpace.Run.Persistence;

public sealed record BattleUnitStateDto(
	ShipPersistenceDto Ship,
	Coord Position,
	Coord Fore,
	Coord Dorsal,
	Coord Starboard,
	int ActionPoints,
	int FuelRemaining,
	string ParentId,
	bool ApPenaltyNextTurn,
	int MaxAp,
	VoidBombProjectile? Projectile,
	IReadOnlyList<MountRuntimeDto> MountRuntime,
	int? ManeuverPoints = null,
	int? MaxMp = null,
	int? Initiative = null,
	int PendingApPenalty = 0,
	FaceShieldDto? PendingShieldRestore = null,
	bool ShieldsUpUsedThisTurn = false,
	IReadOnlyList<EPilotSkillId>? PilotSkills = null);
