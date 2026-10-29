using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;

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
		AbsorbWithShields(unit, face, damage + shieldDamageBonus);
		var hullDamage = System.Math.Max(damage - shieldBefore, 0);
		unit.HullPoints = System.Math.Max(unit.HullPoints - hullDamage, 0);
	}

	public static void ApplyInTheFrayReduction(
		BattleWorld world,
		Unit target,
		ref int appliedDamage,
		ref int appliedShieldDamageBonus)
	{
		if (!target.State.InTheFrayActive || !InTheFrayPositionMet(world, target))
			return;

		var retained = PilotSkillPercents.InTheFray.DamageTakenPercent;
		var reducedShieldDamage = ScalePercent(appliedDamage + appliedShieldDamageBonus, retained);
		appliedDamage = ScalePercent(appliedDamage, retained);
		appliedShieldDamageBonus = reducedShieldDamage - appliedDamage;
	}

	private static bool InTheFrayPositionMet(BattleWorld world, Unit unit)
	{
		var distantEnemies = 0;
		foreach (var other in world.UnitRegistry.All)
		{
			if (!other.State.IsAlive || unit.RelationTo(other) != EUnitRelation.Opponent)
				continue;
			if (unit.State.Position.ManhattanDistanceTo(other.State.Position)
				< PilotSkillPercents.InTheFray.MinimumDistanceCells)
				continue;

			distantEnemies++;
			if (distantEnemies >= PilotSkillPercents.InTheFray.MinimumEnemyCount)
				return true;
		}

		return false;
	}

	private static int ScalePercent(int amount, int percent) =>
		(int)System.Math.Round(amount * percent / 100m, System.MidpointRounding.AwayFromZero);
}
