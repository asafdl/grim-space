using GrimSpace.Battle.Effects;
using GrimSpace.Battle.World;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.Statuses;

public sealed class InTheFray : BattleStatus
{
	public override EStatusPolarity Polarity => EStatusPolarity.Buff;

	public sealed class Rule : TargetDamageReduction<InTheFray>
	{
		protected override int RetainPercent => PilotSkillPercents.InTheFray.DamageTakenPercent;

		protected override bool Applies(BattleEffectContext context, DamageUnitEffect damage) =>
			context.World.CountUnitsInRange(
				damage.TargetId,
				EUnitRelation.Opponent,
				PilotSkillPercents.InTheFray.MinimumDistanceCells)
				>= PilotSkillPercents.InTheFray.MinimumEnemyCount;
	}
}
