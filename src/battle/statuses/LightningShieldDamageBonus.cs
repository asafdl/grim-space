using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.Statuses;

public sealed class LightningShieldDamageBonus : BattleStatus
{
	public override EStatusPolarity Polarity => EStatusPolarity.Buff;

	public sealed class Rule : DamageRule
	{
		protected override bool Matches(BattleEffectContext context, DamageUnitEffect damage) =>
			Attacker(context).Has<LightningShieldDamageBonus>()
			&& IsTagged(context, BattleActionTags.Attack, BattleActionTags.Lightning);

		protected override DamageUnitEffect Transform(BattleEffectContext context, DamageUnitEffect damage) =>
			damage with { ShieldDamageBonusPercent = PilotSkillPercents.IonizedPayload.ShieldDamageBonusPercent };
	}
}
