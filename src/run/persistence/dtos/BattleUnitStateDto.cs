using GrimSpace.Battle;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
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
	int MaxAp,
	VoidBombProjectile? Projectile,
	IReadOnlyList<MountRuntimeDto> MountRuntime,
	int? ManeuverPoints = null,
	int? MaxMp = null,
	int? Initiative = null,
	IReadOnlyList<TemporaryShieldGrantDto>? TemporaryShieldGrants = null,
	bool ShieldsUpUsedThisTurn = false);
