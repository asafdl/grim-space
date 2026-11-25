using GrimSpace.Battle.Effects;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.Statuses;

public sealed class HealPenalty : BattleStatus
{
	public override EStatusPolarity Polarity => EStatusPolarity.Debuff;

	public sealed class Rule : HealerRule<HealPenalty>
	{
		protected override ApplyAreaAllyHealEffect Transform(ApplyAreaAllyHealEffect heal) =>
			heal.WithMultipliedHealRetainPercent(PilotSkillPercents.RoughPatchJob.HealTakenPercent);
	}
}
