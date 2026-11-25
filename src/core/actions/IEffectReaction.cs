using GrimSpace.Core.Engine;

namespace GrimSpace.Core.Actions;

public interface IEffectReaction<TWorld, TRuntime>
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext<TRuntime>
{
	IReadOnlyList<IEffect<TWorld, TRuntime>> React(
		EffectContext<TWorld, TRuntime> context,
		IEffect<TWorld, TRuntime> effect,
		IReadOnlyList<IRecord> records);
}
