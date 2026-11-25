using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public abstract class HealerRule<TStatus> : IEffectRule<BattleWorld, ActorRuntime> where TStatus : BattleStatus
{
	public bool Matches(BattleEffectContext context, IEffect<BattleWorld, ActorRuntime> effect) =>
		effect is ApplyAreaAllyHealEffect && context.World.StateOf(context.Action.ActorId).Has<TStatus>();

	public IEffect<BattleWorld, ActorRuntime> Transform(BattleEffectContext context, IEffect<BattleWorld, ActorRuntime> effect) =>
		Transform((ApplyAreaAllyHealEffect)effect);

	protected abstract ApplyAreaAllyHealEffect Transform(ApplyAreaAllyHealEffect heal);
}
