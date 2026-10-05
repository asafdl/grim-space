using GrimSpace.Math.Grid;
using GrimSpace.World.Factions;

namespace GrimSpace.World.StarSystem.Units;

public sealed record Spawn(
	string Id,
	EType Type,
	string DockedAtDockId,
	Coord IdleCoord,
	double SpeedPerTick,
	double EngageRadius,
	double VisionRadius,
	IReadOnlyList<string> ChoreDockIds,
	EFaction Faction = EFaction.TheOptimality,
	int PatrolRadius = 0);
