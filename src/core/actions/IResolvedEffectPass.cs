using GrimSpace.Core.Engine;

namespace GrimSpace.Core.Actions;

public interface IResolvedEffectPass<TWorld, TRuntime>
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext<TRuntime>
{
	IReadOnlyList<IEffect<TWorld, TRuntime>> RewriteResolvedEffects(
		IAction action,
		TWorld world,
		TRuntime runtime,
		IReadOnlyList<IEffect<TWorld, TRuntime>> effects);
}
