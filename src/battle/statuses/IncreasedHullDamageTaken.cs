using GrimSpace.Battle.Effects;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.Statuses;

public sealed class IncreasedHullDamageTaken : BattleStatus
{
	public override EStatusPolarity Polarity => EStatusPolarity.Debuff;

	public sealed class Rule : TargetHullDamageScale<IncreasedHullDamageTaken>
	{
		protected override int HullDamagePercent => PilotSkillPercents.AsteroidSurfer.HullDamageTakenPercent;
	}
}
