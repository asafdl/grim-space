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
		var visited = new Dictionary<object, int[]>();
		SearchContext? context = input.ShouldPrune is not null ? new SearchContext() : null;

		foreach (var frame in SearchDfs(
			fork,
			actorId,
			actionDefs,
			startDepth,
			0,
			visited,
			input,
			context))
			yield return frame;
	}

	private static IEnumerable<SearchFrame<TWorld, TRuntime>> SearchDfs<TEffect, TWorld, TRuntime>(
		Simulation<TWorld, TRuntime> fork,
		string actorId,
		IReadOnlyList<IActionDef<IAction, TWorld, TRuntime, TEffect>> actionDefs,
		int startDepth,
		int depth,
		Dictionary<object, int[]> visited,
		SearchInput<TWorld, TRuntime> input,
		SearchContext? context)
		where TEffect : IEffect<TWorld, TRuntime>
		where TWorld : IWorld<TWorld>
		where TRuntime : IRuntimeContext<TRuntime>, new()
	{
		if (depth > MaxSearchDepth || depth >= HardAbortSearchDepth)
			yield break;

		if (ShouldPruneVisit(visited, input.VisitState, fork, actorId))
			yield break;

		if (context is not null && input.ShouldPrune?.Invoke(fork, actorId, startDepth, context) == true)
			yield break;

		yield return new SearchFrame<TWorld, TRuntime>(
			fork.World.Fork(),
			fork.Runtimes.Fork(),
			fork.Actions.ToList(),
			fork.Actions.Count - startDepth);

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

				foreach (var frame in SearchDfs(
					fork,
					actorId,
					actionDefs,
					startDepth,
					depth + 1,
					visited,
					input,
					context))
					yield return frame;

				fork.Dequeue(checkpoint);
			}
		}
	}

	private static bool ShouldPruneVisit<TWorld, TRuntime>(
		Dictionary<object, int[]> visited,
		Func<Simulation<TWorld, TRuntime>, string, SearchVisitState> visitState,
		Simulation<TWorld, TRuntime> fork,
		string actorId)
		where TWorld : IWorld<TWorld>
		where TRuntime : IRuntimeContext<TRuntime>, new()
	{
		var visit = visitState(fork, actorId);
		if (visit.Budget.Length == 0)
			return !visited.TryAdd(visit.State, []);

		if (!visited.TryGetValue(visit.State, out var seen))
		{
			visited[visit.State] = (int[])visit.Budget.Clone();
			return false;
		}

		if (Dominates(seen, visit.Budget))
			return true;

		for (var i = 0; i < visit.Budget.Length; i++)
			seen[i] = System.Math.Max(seen[i], visit.Budget[i]);

		return false;
	}

	private static bool Dominates(int[] seen, int[] current)
	{
		for (var i = 0; i < current.Length; i++)
		{
			if (seen[i] < current[i])
				return false;
		}

		return true;
	}
}
