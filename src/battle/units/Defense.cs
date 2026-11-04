using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Statuses;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Units;

public static class Defense
{
	public static int EffectiveShield(State unit, ESpatialOrientation face) =>
		unit.CurrentShieldPoints[face] + TemporaryShields.TotalOnFace(unit, face);

	public static int AbsorbWithShields(State unit, ESpatialOrientation face, int amount)
	{
		if (amount <= 0)
			return 0;

		var fromTemporary = TemporaryShields.AbsorbOnFace(unit, face, amount);
		var remaining = amount - fromTemporary;

		var shield = unit.CurrentShieldPoints[face];
		var fromShield = System.Math.Min(shield, remaining);
		unit.CurrentShieldPoints[face] = shield - fromShield;
		remaining -= fromShield;
		var absorbed = amount - remaining;
		ShieldBrace.RecordShieldDamage(unit, face, fromShield);

		return absorbed;
	}

	public static int RestoreShieldsOnFace(State unit, ESpatialOrientation face, int amount)
	{
		if (amount <= 0)
			return 0;

		var missing = unit.Loadout.BaseShieldPoints[face] - unit.CurrentShieldPoints[face];
		if (missing <= 0)
			return 0;

		var applied = System.Math.Min(amount, missing);
		unit.CurrentShieldPoints[face] += applied;
		return applied;
	}

	public static int RestoreHull(State unit, int amount)
	{
		if (amount <= 0)
			return 0;

		var missing = unit.Loadout.BaseHullPoints - unit.CurrentHullPoints;
		if (missing <= 0)
			return 0;

		var applied = System.Math.Min(amount, missing);
		unit.CurrentHullPoints += applied;
		return applied;
	}

	public static ESpatialOrientation MostMissingShieldFace(State unit)
	{
		var bestFace = ESpatialOrientation.Forward;
		var bestMissing = -1;
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
		{
			var missing = unit.Loadout.BaseShieldPoints[face] - unit.CurrentShieldPoints[face];
			if (missing <= bestMissing)
				continue;

			bestMissing = missing;
			bestFace = face;
		}

		return bestFace;
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
		var shieldDamage = damage + shieldDamageBonus;
		var hullDamage = System.Math.Max(damage - shieldBefore, 0);
		shieldDamage = BattleStatuses.RewriteShieldDamage(unit.Statuses, shieldDamage);
		hullDamage = BattleStatuses.RewriteHullDamage(unit.Statuses, hullDamage);

		AbsorbWithShields(unit, face, shieldDamage);
		unit.CurrentHullPoints = System.Math.Max(unit.CurrentHullPoints - hullDamage, 0);
	}

	public static IncomingHit ScaleHitDamageTaken(IncomingHit hit, int retainedPercent)
	{
		var reducedTotal = ScalePercent(hit.Damage + hit.ShieldDamageBonus, retainedPercent);
		var damage = ScalePercent(hit.Damage, retainedPercent);
		return new IncomingHit(damage, reducedTotal - damage);
	}

	public static int ScalePercent(int amount, int percent) =>
		(int)System.Math.Round(amount * percent / 100m, System.MidpointRounding.AwayFromZero);
}
