using GrimSpace.Battle.Effects;
using GrimSpace.Pilots;
using GrimSpace.World.Factions;

namespace GrimSpace.Battle.Statuses;

public sealed class PirateDamageBonus : BattleStatus
{
	public override EStatusPolarity Polarity => EStatusPolarity.Buff;

	public sealed class Rule : AttackerDamageBonus<PirateDamageBonus>
	{
		protected override int BonusPercent(BattleEffectContext context) =>
			PilotSkillPercents.PirateHater.DamageBonusPercent;

		protected override bool Applies(BattleEffectContext context, DamageUnitEffect damage) =>
			context.World.UnitRegistry.UnitOf(damage.TargetId).Faction == EFaction.Pirates;
	}
}
