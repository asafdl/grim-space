using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Ids;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.Statuses;

public sealed class SpawnedUnitDamageBonus : BattleStatus
{
	public override EStatusPolarity Polarity => EStatusPolarity.Buff;

	public sealed class Rule : AttackerDamageBonus<SpawnedUnitDamageBonus>
	{
		protected override int BonusPercent(BattleEffectContext context) =>
			PilotSkillPercents.NotFullyFormed.SpawnedUnitDamageBonusPercent;

		protected override bool Applies(BattleEffectContext context, DamageUnitEffect damage) =>
			damage.Target(context.World).ParentId != BattleActorIds.Rules;
	}
}
