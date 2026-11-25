using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Units;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.Statuses;

public sealed class NothingButMetal : BattleStatus
{
	public override EStatusPolarity Polarity => EStatusPolarity.Buff;

	public sealed class Rule : AttackerDamageBonus<NothingButMetal>
	{
		protected override int BonusPercent(BattleEffectContext context) =>
			PilotSkillPercents.NothingButMetal.DamageBonusPercent;

		protected override bool Applies(BattleEffectContext context, DamageUnitEffect damage) =>
			Defense.EffectiveShield(damage.Target(context.World), damage.Face(context.World)) == 0;
	}
}
