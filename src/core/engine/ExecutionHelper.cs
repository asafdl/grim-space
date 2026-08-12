using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Engine;

public static class ExecutionHelper
{
	public static IReadOnlyList<IEffect<TWorld, TRuntime>> ApplyAndResolve<TWorld, TRuntime>(
		IAction action,
		TWorld world,
		TRuntime runtime,
		List<IRecord>? recordSink = null)
		where TWorld : IWorld<TWorld>
		where TRuntime : IRuntimeContext<TRuntime>
	{
		if (action is not IAction<TWorld, TRuntime> typed)
			return [];

		var effects = typed.Definition.Resolve(action, world, runtime).ToList();
		foreach (var effect in effects)
		{
			var records = effect.Apply(world, runtime, action.ActorId);
			if (recordSink is not null)
				recordSink.AddRange(records);
		}

		return effects;
	}

	public static IReadOnlyList<IRecord> Apply<TWorld, TRuntime>(IAction action, TWorld world, TRuntime runtime)
		where TWorld : IWorld<TWorld>
		where TRuntime : IRuntimeContext<TRuntime>
	{
		if (action is not IAction<TWorld, TRuntime> typed)
			return [];

		var records = new List<IRecord>();
		foreach (var effect in typed.Definition.Resolve(action, world, runtime))
			records.AddRange(effect.Apply(world, runtime, action.ActorId));

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
