using GrimSpace.Core.Engine;

namespace GrimSpace.Core.Actions;

public interface IAction
{
	string OwnerId { get; }

	int? UndoGroup { get; }
}

public interface IAction<TWorld, TRuntime> : IAction
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext<TRuntime>
{
	bool IsLegal(TWorld world, TRuntime runtime);

	IReadOnlyList<IEffect<TWorld, TRuntime>> Resolve(TWorld world, TRuntime runtime);

	void Apply(TWorld world, TRuntime runtime)
	{
		foreach (var effect in Resolve(world, runtime))
			effect.Apply(world, runtime, OwnerId);
	}
}
