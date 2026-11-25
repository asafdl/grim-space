using GrimSpace.Core.Engine;

namespace GrimSpace.Core.Actions;

public readonly record struct EffectContext<TWorld, TRuntime>(IAction Action, TWorld World, TRuntime Runtime)
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext<TRuntime>;

public interface IEffectRule<TWorld, TRuntime>
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext<TRuntime>
{
	bool Matches(EffectContext<TWorld, TRuntime> context, IEffect<TWorld, TRuntime> effect);

	IEffect<TWorld, TRuntime> Transform(EffectContext<TWorld, TRuntime> context, IEffect<TWorld, TRuntime> effect);
}
