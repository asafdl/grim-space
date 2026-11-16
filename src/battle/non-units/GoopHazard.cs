using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.NonUnits;

public sealed class GoopHazard : Hazard
{
	public override IEnumerable<Coord> MovementBlockingCells => [];
	public override IEnumerable<Coord> AbilityBlockingCells => Cells;
}
