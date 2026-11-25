using GrimSpace.Battle.Effects;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.Statuses;

public sealed class YouLittleRat : BattleStatus
{
	public override EStatusPolarity Polarity => EStatusPolarity.Buff;

	public sealed class Rule : TargetDamageReduction<YouLittleRat>
	{
		protected override int RetainPercent => PilotSkillPercents.YouLittleRat.DamageTakenPercent;

		protected override bool Applies(BattleEffectContext context, DamageUnitEffect damage) =>
			damage.Target(context.World).Position.ManhattanDistanceTo(damage.Origin)
				>= PilotSkillPercents.YouLittleRat.MinimumDistanceCells;
	}
}
