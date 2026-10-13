using GrimSpace.Core.Engine;

namespace GrimSpace.Core.Actions;

public interface IActionEffectModifier<TWorld, TRuntime, TEffect>
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext<TRuntime>
	where TEffect : IEffect<TWorld, TRuntime>
{
	TEffect Modify(IAction action, TWorld world, TRuntime runtime, TEffect effect);
}
