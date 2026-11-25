global using BattleEffectContext = GrimSpace.Core.Actions.EffectContext<GrimSpace.Battle.World.BattleWorld, GrimSpace.Battle.Runtime.ActorRuntime>;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public abstract class DamageRule : IEffectRule<BattleWorld, ActorRuntime>
{
	bool IEffectRule<BattleWorld, ActorRuntime>.Matches(BattleEffectContext context, IEffect<BattleWorld, ActorRuntime> effect) =>
		effect is DamageUnitEffect damage && Matches(context, damage);

	IEffect<BattleWorld, ActorRuntime> IEffectRule<BattleWorld, ActorRuntime>.Transform(
		BattleEffectContext context,
		IEffect<BattleWorld, ActorRuntime> effect) =>
		Transform(context, (DamageUnitEffect)effect);

	protected abstract bool Matches(BattleEffectContext context, DamageUnitEffect damage);

	protected abstract DamageUnitEffect Transform(BattleEffectContext context, DamageUnitEffect damage);

	protected static State Attacker(BattleEffectContext context) => context.World.StateOf(context.Action.ActorId);

	protected static bool IsTagged(BattleEffectContext context, params string[] tags) =>
		tags.All(context.Action.Tags.Contains);
}

public abstract class AttackerDamageBonus<TStatus> : DamageRule where TStatus : BattleStatus
{
	protected abstract int BonusPercent(BattleEffectContext context);

	protected virtual bool Applies(BattleEffectContext context, DamageUnitEffect damage) => true;

	protected sealed override bool Matches(BattleEffectContext context, DamageUnitEffect damage) =>
		Attacker(context).Has<TStatus>() && Applies(context, damage);

	protected sealed override DamageUnitEffect Transform(BattleEffectContext context, DamageUnitEffect damage) =>
		damage.AddBonusPercent(BonusPercent(context));
}

public abstract class TargetDamageReduction<TStatus> : DamageRule where TStatus : BattleStatus
{
	protected abstract int RetainPercent { get; }

	protected virtual bool Applies(BattleEffectContext context, DamageUnitEffect damage) => true;

	protected sealed override bool Matches(BattleEffectContext context, DamageUnitEffect damage) =>
		damage.Target(context.World).Has<TStatus>() && Applies(context, damage);

	protected sealed override DamageUnitEffect Transform(BattleEffectContext context, DamageUnitEffect damage) =>
		damage.ReduceHit(RetainPercent);
}

public abstract class TargetHullDamageScale<TStatus> : DamageRule where TStatus : BattleStatus
{
	protected abstract int HullDamagePercent { get; }

	protected sealed override bool Matches(BattleEffectContext context, DamageUnitEffect damage) =>
		damage.Target(context.World).Has<TStatus>();

	protected sealed override DamageUnitEffect Transform(BattleEffectContext context, DamageUnitEffect damage) =>
		damage.ScaleHullDamage(HullDamagePercent);
}
