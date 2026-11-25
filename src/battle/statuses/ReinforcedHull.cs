using GrimSpace.Battle.Effects;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.Statuses;

public sealed class ReinforcedHull : BattleStatus
{
	public override EStatusPolarity Polarity => EStatusPolarity.Buff;

	public sealed class Rule : TargetHullDamageScale<ReinforcedHull>
	{
		protected override int HullDamagePercent => PilotSkillPercents.ReinforcedHull.HullDamageTakenPercent;
	}
}
