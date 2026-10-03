using GrimSpace.Math.Grid;

namespace GrimSpace.Run.Persistence;

public sealed record BattleEncounterHazardDto(
	Coord Origin,
	IReadOnlyList<Coord> Cells);
