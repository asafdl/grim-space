using GrimSpace.Math.Grid;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Encounter;

namespace GrimSpace.World.StarSystem.Units;

public sealed record Spawn(
	string Id,
	EType Type,
	string DockedAtDockId,
	Coord IdleCoord,
	double SpeedPerTick,
	double EngageRadius,
	IReadOnlyList<string> ChoreDockIds,
	EFaction Faction = EFaction.TheOptimality,
	CombatProfile? CombatProfile = null);
