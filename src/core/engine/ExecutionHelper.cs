using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Engine;

public static class ExecutionHelper
{
	public const int MaxReactionDepth = 8;

	public static IReadOnlyList<IEffect<TWorld, TRuntime>> ApplyAndResolve<TWorld, TRuntime>(
		IAction action,
		TWorld world,
		TRuntime runtime,
		EffectRules<TWorld, TRuntime> rules,
		List<IRecord>? recordSink = null)
		where TWorld : IWorld<TWorld>
		where TRuntime : IRuntimeContext<TRuntime>
	{
		ArgumentNullException.ThrowIfNull(rules);
		if (action is not IAction<TWorld, TRuntime> typed)
			return [];

		var applied = new List<IEffect<TWorld, TRuntime>>();
		foreach (var intent in typed.Definition.Resolve(action, world, runtime))
		{
			var prepared = rules.Prepare(action, world, runtime, intent);
			ApplyPrepared(action, world, runtime, rules, prepared, 0, applied, recordSink);
		}

		return applied;
	}

	private static void ApplyPrepared<TWorld, TRuntime>(
		IAction action,
		TWorld world,
		TRuntime runtime,
		EffectRules<TWorld, TRuntime> rules,
		IEffect<TWorld, TRuntime> effect,
		int depth,
		List<IEffect<TWorld, TRuntime>> applied,
		List<IRecord>? recordSink)
		where TWorld : IWorld<TWorld>
		where TRuntime : IRuntimeContext<TRuntime>
	{
		var records = effect.Apply(world, runtime, action.ActorId);
		applied.Add(effect);
		recordSink?.AddRange(records);

		var followUps = rules.React(action, world, runtime, effect, records);
		if (followUps.Count > 0 && depth == MaxReactionDepth)
			throw new InvalidOperationException($"Effect reactions exceeded the maximum depth of {MaxReactionDepth}.");

		foreach (var followUp in followUps)
		{
			var prepared = rules.Prepare(action, world, runtime, followUp);
			ApplyPrepared(action, world, runtime, rules, prepared, depth + 1, applied, recordSink);
		}
	}

	public static IReadOnlyList<IRecord> Apply<TWorld, TRuntime>(
		IAction action,
		TWorld world,
		TRuntime runtime,
		EffectRules<TWorld, TRuntime> rules)
		where TWorld : IWorld<TWorld>
		where TRuntime : IRuntimeContext<TRuntime>
	{
		var records = new List<IRecord>();
		ApplyAndResolve(action, world, runtime, rules, records);
		return records;
	}

	public static void UndoEffects<TWorld, TRuntime>(
		IReadOnlyList<IEffect<TWorld, TRuntime>> effects,
		IAction action,
		TWorld world,
		TRuntime runtime)
		where TWorld : IWorld<TWorld>
		where TRuntime : IRuntimeContext<TRuntime>
	{
		for (var i = effects.Count - 1; i >= 0; i--)
			effects[i].Undo(world, runtime, action.ActorId);
	}
}
