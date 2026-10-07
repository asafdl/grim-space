using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.NonUnits;

public sealed class GoopHazard : Hazard
{
	public override bool Passable => true;
	public override bool BlocksAbilities => true;
}
