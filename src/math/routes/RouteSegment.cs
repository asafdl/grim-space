using GrimSpace.Math.Grid;

namespace GrimSpace.Math.Routes;

public readonly record struct RouteSegment(
	IReadOnlyList<Coord> Points,
	double Length,
	double SpeedMultiplier);
