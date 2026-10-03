using GrimSpace.Math.Grid;

namespace GrimSpace.Run.Persistence;

internal sealed record TransitLegDto(
	IReadOnlyList<Coord> Points,
	double SpeedMultiplier,
	double Length);
