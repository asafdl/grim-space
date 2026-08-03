using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.Core.Dfs;

public sealed class SearchContext
{
	public int BestScore { get; set; } = int.MinValue;
}

public readonly record struct SearchInput<TWorld, TRuntime>(
	Func<Simulation<TWorld, TRuntime>, string, SearchVisitState> VisitState,
	Func<Simulation<TWorld, TRuntime>, string, int, SearchContext, bool>? ShouldPrune = null)
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext<TRuntime>, new();
