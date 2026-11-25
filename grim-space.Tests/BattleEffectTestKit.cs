using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.Tests;

internal static class BattleEffectTestKit
{
	public static DamageUnitEffect PrepareDamage(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime,
		DamageUnitEffect damage) =>
		(DamageUnitEffect)BattleEffectRules.Rules.Prepare(action, world, runtime, damage);

	public static ApplyAreaAllyHealEffect PrepareHeal(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime,
		ApplyAreaAllyHealEffect heal) =>
		(ApplyAreaAllyHealEffect)BattleEffectRules.Rules.Prepare(action, world, runtime, heal);

	public static IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> ApplyEffects(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime,
		IEnumerable<IEffect<BattleWorld, ActorRuntime>> effects,
		List<IRecord>? records = null) =>
		ExecutionHelper.ApplyAndResolve(
			new ScriptedEffectsAction(action, effects.ToArray()),
			world,
			runtime,
			BattleEffectRules.Rules,
			records);

	private sealed class ScriptedEffectsAction : IAction<BattleWorld, ActorRuntime>
	{
		private readonly IAction _source;
		private readonly IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> _effects;

		public ScriptedEffectsAction(
			IAction source,
			IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> effects)
		{
			_source = source;
			_effects = effects;
		}

		public string ActorId => _source.ActorId;

		public IReadOnlyList<string> Tags => _source.Tags;

		public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
			new ScriptedEffectsDef(_effects);
	}

	private sealed class ScriptedEffectsDef(IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> effects)
		: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
	{
		public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId) => [];

		public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) => true;

		public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) => true;

		public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
			IAction action,
			BattleWorld world,
			ActorRuntime runtime) =>
			effects;
	}
}
