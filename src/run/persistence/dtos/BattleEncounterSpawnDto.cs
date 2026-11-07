using GrimSpace.Battle;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.World.Factions;

namespace GrimSpace.Run.Persistence;

public sealed record BattleEncounterSpawnDto(
	ShipPersistenceDto Ship,
	PilotBattleSnapshotDto? Pilot,
	ETeam Team,
	EFaction Faction,
	Coord Position,
	Coord Fore,
	Coord Dorsal,
	EBattleAgentKind AgentKind);
