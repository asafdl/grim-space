using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Engine;

public readonly record struct SearchFrame<TWorld, TRuntime>(
	TWorld World,
	ActorRuntimes<TRuntime> Runtimes,
	IReadOnlyList<IAction> Actions,
	int Depth)
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext<TRuntime>, new();
