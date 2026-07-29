using GrimSpace.Battle.Movement.Enums;

namespace GrimSpace.Battle.Units;

public static class Defense
{
	public static void ApplyDamage(State unit, int damage, ESpatialOrientation face)
	{
		if (damage <= 0)
			return;

		var remaining = damage;
		var shield = unit.ShieldPoints[face];
		var absorbed = System.Math.Min(shield, remaining);
		unit.ShieldPoints[face] = shield - absorbed;
		remaining -= absorbed;
		unit.HullPoints = System.Math.Max(unit.HullPoints - remaining, 0);
	}
}
