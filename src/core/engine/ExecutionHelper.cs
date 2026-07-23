using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Engine;

public static class ExecutionHelper
{
	public static void Apply<TWorld, TRuntime>(IAction action, TWorld world, TRuntime runtime)
		where TWorld : IWorld<TWorld>
		where TRuntime : IRuntimeContext<TRuntime>
	{
		if (action is not IAction<TWorld, TRuntime> typed)
			return;

		foreach (var effect in typed.Definition.Resolve(action, world, runtime))
			effect.Apply(world, runtime, action.ActorId);
	}
}
