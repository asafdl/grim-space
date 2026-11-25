using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Units;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.Statuses;

public sealed class SpawnedAllyDamageBuff : BattleStatus
{
	public override EStatusPolarity Polarity => EStatusPolarity.Buff;

	public static void GrantFrom(State parent, State child)
	{
		if (!parent.Has<SpawnedAllyDamageBonus>())
			return;
		if (child.Has<SpawnedAllyDamageBuff>())
			return;
		child.Statuses.Add(new SpawnedAllyDamageBuff());
	}

	public sealed class Rule : AttackerDamageBonus<SpawnedAllyDamageBuff>
	{
		protected override int BonusPercent(BattleEffectContext context) =>
			PilotSkillPercents.RetiredMachineManager.SpawnedAllyDamageBonusPercent;
	}
}
