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
		int depth)
	{
		World = world;
		Runtimes = runtimes;
		Actions = actions;
		Depth = depth;
	}

	public TWorld World { get; }
	public ActorRuntimes<TRuntime> Runtimes { get; }
	public IReadOnlyList<IAction> Actions { get; }
	public int Depth { get; }

	/// <summary>
	/// When set by the consumer after a yield, stops DFS expansion of this frame's children.
	/// Does not un-yield this frame.
	/// </summary>
	public bool PruneChildren { get; set; }
}
