using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.Core.Dfs;

public readonly record struct SearchFrame<TWorld, TRuntime>(
	TWorld World,
	ActorRuntimes<TRuntime> Runtimes,
	IReadOnlyList<IAction> Actions,
	int Depth)
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext<TRuntime>, new();
