using GrimSpace.Battle.Effects;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.Statuses;

public sealed class OutgoingDamageBonus : BattleStatus
{
	public override EStatusPolarity Polarity => EStatusPolarity.Buff;

	public sealed class Rule : AttackerDamageBonus<OutgoingDamageBonus>
	{
		protected override int BonusPercent(BattleEffectContext context) =>
			PilotSkillPercents.AsteroidSurfer.OutgoingDamageBonusPercent;
	}
}
