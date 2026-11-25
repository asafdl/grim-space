using GrimSpace.Battle.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class OverdriveDamageRule : DamageRule
{
	protected override bool Matches(BattleEffectContext context, DamageUnitEffect damage) =>
		IsTagged(context, BattleActionTags.Attack) && Attacker(context).Overdrive;

	protected override DamageUnitEffect Transform(BattleEffectContext context, DamageUnitEffect damage) =>
		damage.AddBonusPercent(Attacker(context).Loadout.OverdriveDamageBonusPercent);
}
