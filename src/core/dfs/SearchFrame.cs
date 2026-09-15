using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.Core.Dfs;

public sealed class SearchFrame<TWorld, TRuntime>
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext<TRuntime>, new()
{
	public SearchFrame(
		TWorld world,
		ActorRuntimes<TRuntime> runtimes,
		IReadOnlyList<IAction> actions,
		int depth,
		SearchFramePhase phase = SearchFramePhase.Remaining)
	{
		World = world;
		Runtimes = runtimes;
		Actions = actions;
		Depth = depth;
		Phase = phase;
	}

	public TWorld World { get; }
	public ActorRuntimes<TRuntime> Runtimes { get; }
	public IReadOnlyList<IAction> Actions { get; }
	public int Depth { get; }
	public SearchFramePhase Phase { get; }

	/// <summary>
	/// When set by the consumer after a yield, stops DFS expansion of this frame's children.
	/// Does not un-yield this frame.
	/// </summary>
	public bool PruneChildren { get; set; }
}

public enum SearchFramePhase
{
	Priority,
	Remaining,
}
