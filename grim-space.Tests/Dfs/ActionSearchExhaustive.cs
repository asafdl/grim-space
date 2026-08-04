using GrimSpace.Core.Actions;
using GrimSpace.Core.Dfs;
using GrimSpace.Core.Engine;

namespace GrimSpace.Tests.Dfs;

/// <summary>
/// Unpruned action search used to verify pruned <see cref="ActionSearch"/> results.
/// </summary>
internal static class ActionSearchExhaustive
{
	public const int DefaultMaxDepth = 12;

	public static IEnumerable<SearchFrame<TWorld, TRuntime>> Run<TEffect, TWorld, TRuntime>(
		Simulation<TWorld, TRuntime> sim,
		string actorId,
		IReadOnlyList<IActionDef<IAction, TWorld, TRuntime, TEffect>> actionDefs,
		int maxDepth = DefaultMaxDepth)
		where TEffect : IEffect<TWorld, TRuntime>
		where TWorld : IWorld<TWorld>
		where TRuntime : IRuntimeContext<TRuntime>, new()
	{
		var fork = sim.Fork();
		var startDepth = fork.Actions.Count;
		return SearchDfs(fork, actorId, actionDefs, startDepth, 0, maxDepth);
	}

	private static IEnumerable<SearchFrame<TWorld, TRuntime>> SearchDfs<TEffect, TWorld, TRuntime>(
		Simulation<TWorld, TRuntime> fork,
		string actorId,
		IReadOnlyList<IActionDef<IAction, TWorld, TRuntime, TEffect>> actionDefs,
		int startDepth,
		int depth,
		int maxDepth)
		where TEffect : IEffect<TWorld, TRuntime>
		where TWorld : IWorld<TWorld>
		where TRuntime : IRuntimeContext<TRuntime>, new()
	{
		if (depth > maxDepth)
			yield break;

		yield return new SearchFrame<TWorld, TRuntime>(
			fork.World.Fork(),
			fork.Runtimes.Fork(),
			fork.Actions.ToList(),
			fork.Actions.Count - startDepth);

		foreach (var def in actionDefs)
		{
			var runtime = fork.Runtimes.For(actorId);
			foreach (var candidate in def.Discover(fork.World, runtime, actorId))
			{
				var checkpoint = fork.Actions.Count;
				if (!fork.TryEnqueue(candidate))
					continue;

				if (fork.InvariantStatus == InvariantStatus.Impossible)
				{
					fork.Dequeue(checkpoint);
					continue;
				}

				foreach (var frame in SearchDfs(
					fork,
					actorId,
					actionDefs,
					startDepth,
					depth + 1,
					maxDepth))
					yield return frame;

				fork.Dequeue(checkpoint);
			}
		}
	}
}
