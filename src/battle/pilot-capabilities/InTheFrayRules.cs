using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.PilotCapabilities;

public static class InTheFrayRules
{
	public static bool MeetsPositionCondition(BattleWorld world, Unit unit)
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

	public static bool AppliesDamageReduction(BattleWorld world, Unit target) =>
		target.State.InTheFrayActive && MeetsPositionCondition(world, target);

	public static void ReduceIncomingDamage(ref int appliedDamage, ref int appliedShieldDamageBonus)
	{
		var retained = PilotSkillPercents.InTheFray.DamageTakenPercent;
		var reducedShieldDamage = ScalePercent(appliedDamage + appliedShieldDamageBonus, retained);
		appliedDamage = ScalePercent(appliedDamage, retained);
		appliedShieldDamageBonus = reducedShieldDamage - appliedDamage;
	}

	private static int ScalePercent(int amount, int percent) =>
		(int)System.Math.Round(amount * percent / 100m, System.MidpointRounding.AwayFromZero);
}
