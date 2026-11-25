using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.Statuses;

public sealed class ScrapDroneSwarmDamageBonus : BattleStatus
{
	public override EStatusPolarity Polarity => EStatusPolarity.Buff;

	public sealed class Rule : AttackerDamageBonus<ScrapDroneSwarmDamageBonus>
	{
		protected override int BonusPercent(BattleEffectContext context) =>
			PilotSkillPercents.ScrapDrone.SwarmDamageBonusPercent;

		protected override bool Applies(BattleEffectContext context, DamageUnitEffect damage) =>
			IsTagged(context, BattleActionTags.Attack, BattleActionTags.ScrapDrone);
	}
}
