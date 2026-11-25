using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.Statuses;

public sealed class ThirstForScrap : BattleStatus
{
	public override EStatusPolarity Polarity => EStatusPolarity.Buff;
	public int Stacks { get; set; }

	public override BattleStatus DeepCopy() => new ThirstForScrap { Stacks = Stacks };

	public sealed class DamageBonus : AttackerDamageBonus<ThirstForScrap>
	{
		protected override int BonusPercent(BattleEffectContext context) =>
			Attacker(context).StatusOf<ThirstForScrap>()!.Stacks
			* PilotSkillPercents.ThirstForScrap.DamageBonusPercentPerKill;
	}

	public sealed class KillStackReaction : IEffectReaction<BattleWorld, ActorRuntime>
	{
		public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> React(
			BattleEffectContext context,
			IEffect<BattleWorld, ActorRuntime> effect,
			IReadOnlyList<IRecord> records)
		{
			if (effect is not DamageUnitEffect
				|| records is not [Record<ImpactFacts> { Value: { IsLethal: true } impact }])
				return [];

			return KillCredit.Credited(context.World, impact)
				.Where(state => state.Has<ThirstForScrap>())
				.Select(state => new GainThirstForScrapStackEffect(state.Id))
				.ToArray();
		}
	}
}
