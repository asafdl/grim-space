using GrimSpace.Battle.Effects;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.Statuses;

public sealed class DamageResistance : BattleStatus
{
	public override EStatusPolarity Polarity => EStatusPolarity.Buff;

	public sealed class Rule : DamageRule
	{
		protected override bool Matches(BattleEffectContext context, DamageUnitEffect damage) =>
			damage.Target(context.World).Has<DamageResistance>();

		protected override DamageUnitEffect Transform(BattleEffectContext context, DamageUnitEffect damage)
		{
			var times = damage.Target(context.World).Statuses.OfType<DamageResistance>().Count();
			return damage.ReduceHit(PilotSkillPercents.HydraulicAbuser.DamageTakenPercent, times);
		}
	}
}
