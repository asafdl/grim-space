using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.Core.Dfs;

public readonly record struct SearchInput<TWorld, TRuntime>(
	Func<Simulation<TWorld, TRuntime>, string, SearchVisitState> VisitState)
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext<TRuntime>, new();
