using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.NonUnits;

public abstract class Hazard : NonUnit
{
	public required Coord Center { get; init; }
}
