using GrimSpace.Battle.Effects;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.Statuses;

public sealed class HealBonus : BattleStatus
{
	public override EStatusPolarity Polarity => EStatusPolarity.Buff;

	public sealed class Rule : HealerRule<HealBonus>
	{
		protected override ApplyAreaAllyHealEffect Transform(ApplyAreaAllyHealEffect heal) =>
			heal.WithAddedHealBonusPercent(PilotSkillPercents.ICanFixThat.HealBonusPercent);
	}
}
