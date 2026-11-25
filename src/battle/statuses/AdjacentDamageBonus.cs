using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.Statuses;

public sealed class AdjacentDamageBonus : BattleStatus
{
	public override EStatusPolarity Polarity => EStatusPolarity.Buff;

	public sealed class Rule : DamageRule
	{
		protected override bool Matches(BattleEffectContext context, DamageUnitEffect damage) =>
			Attacker(context).Has<AdjacentDamageBonus>()
			&& IsTagged(context, BattleActionTags.Attack)
			&& damage.Cause != EImpactCause.LightningCannonBurst
			&& damage.Origin.ManhattanDistanceTo(damage.Target(context.World).Position)
				== PilotSkillPercents.DodgeThis.BonusRangeCells;

		protected override DamageUnitEffect Transform(BattleEffectContext context, DamageUnitEffect damage) =>
			damage.ScaleDamage(PilotSkillPercents.DodgeThis.BonusHitDamagePercent);
	}
}
