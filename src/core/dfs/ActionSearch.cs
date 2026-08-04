using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.Core.Dfs;

public static class ActionSearch
{
	private const int MaxSearchDepth = 12;
	private const int HardAbortSearchDepth = 64;

	public static IEnumerable<SearchFrame<TWorld, TRuntime>> Run<TEffect, TWorld, TRuntime>(
		Simulation<TWorld, TRuntime> sim,
		string actorId,
		IReadOnlyList<IActionDef<IAction, TWorld, TRuntime, TEffect>> actionDefs,
		Func<Simulation<TWorld, TRuntime>, string, SearchVisitState> visitState)
		where TEffect : IEffect<TWorld, TRuntime>
		where TWorld : IWorld<TWorld>
		where TRuntime : IRuntimeContext<TRuntime>, new() =>
		Run(sim, actorId, actionDefs, new SearchInput<TWorld, TRuntime>(visitState));

	public static IEnumerable<SearchFrame<TWorld, TRuntime>> Run<TEffect, TWorld, TRuntime>(
		Simulation<TWorld, TRuntime> sim,
		string actorId,
		IReadOnlyList<IActionDef<IAction, TWorld, TRuntime, TEffect>> actionDefs,
		SearchInput<TWorld, TRuntime> input)
		where TEffect : IEffect<TWorld, TRuntime>
		where TWorld : IWorld<TWorld>
		where TRuntime : IRuntimeContext<TRuntime>, new()
	{
		var fork = sim.Fork();
		var startDepth = fork.Actions.Count;
		var visited = new Dictionary<object, List<int[]>>();

		foreach (var frame in SearchDfs(
			fork,
			actorId,
			actionDefs,
			startDepth,
			0,
			visited,
			input))
			yield return frame;
	}

	private static IEnumerable<SearchFrame<TWorld, TRuntime>> SearchDfs<TEffect, TWorld, TRuntime>(
		Simulation<TWorld, TRuntime> fork,
		string actorId,
		IReadOnlyList<IActionDef<IAction, TWorld, TRuntime, TEffect>> actionDefs,
		int startDepth,
		int depth,
		Dictionary<object, List<int[]>> visited,
		SearchInput<TWorld, TRuntime> input)
		where TEffect : IEffect<TWorld, TRuntime>
		where TWorld : IWorld<TWorld>
		where TRuntime : IRuntimeContext<TRuntime>, new()
	{
		if (depth > MaxSearchDepth || depth >= HardAbortSearchDepth)
			yield break;

		if (ShouldPruneVisit(visited, input.VisitState, fork, actorId))
			yield break;

		var frame = new SearchFrame<TWorld, TRuntime>(
			fork.World.Fork(),
			fork.Runtimes.Fork(),
			fork.Actions.ToList(),
			fork.Actions.Count - startDepth);

		yield return frame;

		var pruneChildren = frame.PruneChildren;
		frame = null!;

		if (pruneChildren)
			yield break;

		foreach (var def in actionDefs)
		{
			var runtime = fork.Runtimes.For(actorId);
			var candidates = def.Discover(fork.World, runtime, actorId).ToList();

			foreach (var candidate in candidates)
			{
				var checkpoint = fork.Actions.Count;
				if (!fork.TryEnqueue(candidate))
					continue;

				if (fork.InvariantStatus == InvariantStatus.Impossible)
				{
					fork.Dequeue(checkpoint);
					continue;
				}

				foreach (var child in SearchDfs(
					fork,
					actorId,
					actionDefs,
					startDepth,
					depth + 1,
					visited,
					input))
					yield return child;

				fork.Dequeue(checkpoint);
			}
		}
	}

	private static bool ShouldPruneVisit<TWorld, TRuntime>(
		Dictionary<object, List<int[]>> visited,
		Func<Simulation<TWorld, TRuntime>, string, SearchVisitState> visitState,
		Simulation<TWorld, TRuntime> fork,
		string actorId)
		where TWorld : IWorld<TWorld>
		where TRuntime : IRuntimeContext<TRuntime>, new()
	{
		var visit = visitState(fork, actorId);
		if (visit.Budget.Length == 0)
			return !TryAddEmptyBudget(visited, visit.State);

		if (!visited.TryGetValue(visit.State, out var frontier))
		{
			visited[visit.State] = [(int[])visit.Budget.Clone()];
			return false;
		}

		return BudgetFrontier.ShouldPrune(frontier, visit.Budget);
	}

	private static bool TryAddEmptyBudget(Dictionary<object, List<int[]>> visited, object state)
	{
		if (visited.ContainsKey(state))
			return false;

		visited[state] = [];
		return true;
	}
}
