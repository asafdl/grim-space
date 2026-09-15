using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.Core.Dfs;

public static class ActionSearch
{
	private const int MaxSearchDepth = 20;
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
		var isPrioritySearch = input.IsPriorityBranch?.Invoke(fork.Actions) ?? false;
		Queue<(Simulation<TWorld, TRuntime> Sim, int Depth)>? deferred =
			isPrioritySearch ? new() : null;

		foreach (var frame in SearchDfs(
			fork,
			actorId,
			actionDefs,
			startDepth,
			0,
			visited,
			input,
			isPrioritySearch ? SearchFramePhase.Priority : SearchFramePhase.Remaining,
			deferred))
			yield return frame;

		if (deferred is null)
			yield break;

		while (deferred.TryDequeue(out var branch))
		{
			foreach (var frame in SearchDfs(
				branch.Sim,
				actorId,
				actionDefs,
				startDepth,
				branch.Depth,
				visited,
				input,
				SearchFramePhase.Remaining))
				yield return frame;
		}
	}

	private static IEnumerable<SearchFrame<TWorld, TRuntime>> SearchDfs<TEffect, TWorld, TRuntime>(
		Simulation<TWorld, TRuntime> fork,
		string actorId,
		IReadOnlyList<IActionDef<IAction, TWorld, TRuntime, TEffect>> actionDefs,
		int startDepth,
		int depth,
		Dictionary<object, List<int[]>> visited,
		SearchInput<TWorld, TRuntime> input,
		SearchFramePhase phase,
		Queue<(Simulation<TWorld, TRuntime> Sim, int Depth)>? deferred = null)
		where TEffect : IEffect<TWorld, TRuntime>
		where TWorld : IWorld<TWorld>
		where TRuntime : IRuntimeContext<TRuntime>, new()
	{
		if (depth > MaxSearchDepth || depth >= HardAbortSearchDepth)
			yield break;

		if (ShouldPruneVisit(visited, input.VisitState, fork, actorId))
			yield break;

		var pruneChildren = false;
		if (fork.InvariantStatus == InvariantStatus.Ok)
		{
			var frame = new SearchFrame<TWorld, TRuntime>(
				fork.World.Fork(),
				fork.Runtimes.Fork(),
				fork.Actions.ToList(),
				fork.Actions.Count - startDepth,
				phase);

			yield return frame;
			pruneChildren = frame.PruneChildren;
		}

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

				if (deferred is not null && !input.IsPriorityBranch!(fork.Actions))
				{
					deferred.Enqueue((fork.Fork(), depth + 1));
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
					input,
					phase,
					deferred))
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
