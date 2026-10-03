using GrimSpace.Battle;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Run.Persistence;

public sealed record BattleEncounterSpawnDto(
	ShipPersistenceDto Ship,
	ETeam Team,
	Coord Position,
	Coord Fore,
	Coord Dorsal,
	EBattleAgentKind AgentKind);
