using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Units;

public static class Defense
{
	public static int EffectiveShield(State unit, ESpatialOrientation face) =>
		unit.ShieldPoints[face] + TemporaryShields.TotalOnFace(unit, face);

	public static int AbsorbWithShields(State unit, ESpatialOrientation face, int amount)
	{
		if (amount <= 0)
			return 0;

		var fromTemporary = TemporaryShields.AbsorbOnFace(unit, face, amount);
		var remaining = amount - fromTemporary;

		var shield = unit.ShieldPoints[face];
		var fromShield = System.Math.Min(shield, remaining);
		unit.ShieldPoints[face] = shield - fromShield;
		remaining -= fromShield;

		return amount - remaining;
	}

	public static void ApplyDamage(
		State unit,
		int damage,
		ESpatialOrientation face,
		int shieldDamageBonus = 0)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(shieldDamageBonus);
		if (damage <= 0)
			return;

		var shieldBefore = EffectiveShield(unit, face);
		AbsorbWithShields(unit, face, damage + shieldDamageBonus);
		var hullDamage = System.Math.Max(damage - shieldBefore, 0);
		unit.HullPoints = System.Math.Max(unit.HullPoints - hullDamage, 0);
	}
}
