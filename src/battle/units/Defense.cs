using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Statuses;
using GrimSpace.Math;
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
		var absorbed = amount - remaining;
		ShieldBrace.RecordShieldDamage(unit, face, fromShield);

		return absorbed;
	}

	public static int RestoreShieldsOnFace(State unit, ESpatialOrientation face, int amount)
	{
		if (amount <= 0)
			return 0;

		var missing = unit.Loadout.MaxShieldPoints[face] - unit.ShieldPoints[face];
		if (missing <= 0)
			return 0;

		var applied = System.Math.Min(amount, missing);
		unit.ShieldPoints[face] += applied;
		return applied;
	}

	public static int RestoreHull(State unit, int amount)
	{
		if (amount <= 0)
			return 0;

		var missing = unit.Loadout.MaxHullPoints - unit.HullPoints;
		if (missing <= 0)
			return 0;

		var applied = System.Math.Min(amount, missing);
		unit.HullPoints += applied;
		return applied;
	}

	public static ESpatialOrientation MostMissingShieldFace(State unit)
	{
		var bestFace = ESpatialOrientation.Forward;
		var bestMissing = -1;
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
		{
			var missing = unit.Loadout.MaxShieldPoints[face] - unit.ShieldPoints[face];
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
		unit.HullPoints = System.Math.Max(unit.HullPoints - hullDamage, 0);
	}

	public static IncomingHit RetainHit(IncomingHit hit, int retainedPercent) =>
		new(
			ScalePercent(hit.Damage, retainedPercent),
			ScalePercent(hit.ShieldDamageBonus, retainedPercent));

	public static IncomingHit ScaleHitDamageTaken(IncomingHit hit, int retainedPercent)
	{
		var reducedTotal = ScalePercent(hit.Damage + hit.ShieldDamageBonus, retainedPercent);
		var damage = ScalePercent(hit.Damage, retainedPercent);
		return new IncomingHit(damage, reducedTotal - damage);
	}

	public static bool RollsDeflection(
		State unit,
		int rollIndex,
		EImpactCause cause,
		int chancePercent,
		int? sourceSalt = null)
	{
		var mixer = new StableSeedMixer()
			.Add(unit.Id)
			.Add(rollIndex)
			.Add((int)cause);
		if (sourceSalt is int salt)
			mixer.Add(salt);
		return new StableRandom(mixer.Value).NextDouble() < chancePercent / 100.0;
	}

	public static int ScalePercent(int amount, int percent) =>
		(int)System.Math.Round(amount * percent / 100m, System.MidpointRounding.AwayFromZero);
}
