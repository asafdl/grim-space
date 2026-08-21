using GrimSpace.Math.Grid;

namespace GrimSpace.World.StarSystem.Traffic;

public sealed record Dock(
	string Id,
	string PoiId,
	Coord ArrivalBerth,
	Coord DepartureBerth,
	Coord QueueHold);
