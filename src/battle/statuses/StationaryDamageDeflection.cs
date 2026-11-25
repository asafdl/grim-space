using GrimSpace.Battle.Effects;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.Statuses;

public sealed class StationaryDamageDeflection : BattleStatus
{
	public override EStatusPolarity Polarity => EStatusPolarity.Buff;

	public sealed class Rule : TargetDamageReduction<StationaryDamageDeflection>
	{
		protected override int RetainPercent => PilotSkillPercents.DefensiveManeuvers.DamageTakenPercent;

		protected override bool Applies(BattleEffectContext context, DamageUnitEffect damage) =>
			damage.Target(context.World).DefensiveManeuversMpUsed == 0;
	}
}
