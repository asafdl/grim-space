using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.Statuses;

public sealed class ReactiveShields : BattleStatus
{
	public override EStatusPolarity Polarity => EStatusPolarity.Buff;
	public int Stacks { get; set; }

	public override BattleStatus DeepCopy() => new ReactiveShields { Stacks = Stacks };

	public sealed class DamageReduction : DamageRule
	{
		protected override bool Matches(BattleEffectContext context, DamageUnitEffect damage) =>
			damage.Target(context.World).StatusOf<ReactiveShields>() is { Stacks: > 0 };

		protected override DamageUnitEffect Transform(BattleEffectContext context, DamageUnitEffect damage) =>
			damage.ReduceHit(
				PilotSkillPercents.ReactiveShields.DamageTakenPercentPerStack,
				damage.Target(context.World).StatusOf<ReactiveShields>()!.Stacks);
	}

	public sealed class HitStackReaction : IEffectReaction<BattleWorld, ActorRuntime>
	{
		public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> React(
			BattleEffectContext context,
			IEffect<BattleWorld, ActorRuntime> effect,
			IReadOnlyList<IRecord> records)
		{
			if (effect is not DamageUnitEffect
				|| records is not [Record<ImpactFacts> { Value: var impact }]
				|| !context.World.StateOf(impact.TargetId).Has<ReactiveShields>())
				return [];

			return [new GainReactiveShieldsStackEffect(impact.TargetId)];
		}
	}
}
