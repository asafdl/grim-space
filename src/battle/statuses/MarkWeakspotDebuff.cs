using GrimSpace.Battle.Effects;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.Statuses;

public sealed class MarkWeakspotDebuff : BattleStatus
{
	public override EStatusPolarity Polarity => EStatusPolarity.Debuff;
	public required ESpatialOrientation Face { get; init; }

	public override BattleStatus DeepCopy() => new MarkWeakspotDebuff { Face = Face };

	public sealed class Rule : DamageRule
	{
		protected override bool Matches(BattleEffectContext context, DamageUnitEffect damage) =>
			damage.Target(context.World).StatusOf<MarkWeakspotDebuff>()?.Face == damage.Face(context.World);

		protected override DamageUnitEffect Transform(BattleEffectContext context, DamageUnitEffect damage) =>
			damage.AddShieldDamageOnHit(PilotSkillPercents.MarkWeakspot.ShieldDamageBonusPercent);
	}
}
