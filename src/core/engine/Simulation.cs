using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Engine;

/// <summary>
/// Stateful planning workspace: anchor world, preview fork, per-actor runtimes, and action queue.
/// </summary>
public class Simulation<TWorld, TRuntime>
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext<TRuntime>, new()
{
	private readonly List<IAction> _actions = [];
	private readonly TWorld _anchorWorld;
	private readonly ActorRuntimes<TRuntime> _anchorActorRuntimes;
	private int _anchorTick;
	private int _nextUndoGroup;

	public Simulation(TWorld anchorWorld, ActorRuntimes<TRuntime> anchorActorRuntimes)
	{
		_anchorWorld = anchorWorld;
		_anchorActorRuntimes = anchorActorRuntimes;
	}

	public TWorld PreviewWorld { get; private set; } = default!;

	public ActorRuntimes<TRuntime> PreviewActorRuntimes { get; private set; } = default!;

	public IReadOnlyList<IAction> Actions => _actions;

	public int AnchorTick => _anchorTick;

	public TWorld AnchorWorld => _anchorWorld;

	public ActorRuntimes<TRuntime> AnchorActorRuntimes => _anchorActorRuntimes;

	public int WorldVersion { get; private set; }

	public bool IsStale(int currentWorldVersion) => WorldVersion != currentWorldVersion;

	public void Begin(int anchorTick, int worldVersion)
	{
		_anchorTick = anchorTick;
		WorldVersion = worldVersion;
		_actions.Clear();
		_nextUndoGroup = 0;
		Reevaluate();
	}

	public int AllocateUndoGroup() => ++_nextUndoGroup;

	/// <summary>
	/// Assumes <see cref="PreviewWorld"/> already matches <see cref="Actions"/>.
	/// Callers that mutate preview outside the action list (overlays) must
	/// <see cref="Reevaluate"/> first. Live-world refresh is the engine/session owner's job.
	/// </summary>
	public bool TryEnqueue(IAction action)
	{
		if (action is not IAction<TWorld, TRuntime> typed)
			return false;

		var runtime = PreviewActorRuntimes.For(action);
		if (!typed.Definition.IsLegal(action, PreviewWorld, runtime))
			return false;

		_actions.Add(action);
		ExecutionHelper.Apply(action, PreviewWorld, PreviewActorRuntimes.For(action));
		return true;
	}

	public IEnumerable<TickResult> StepPreview(int ticksToAdvance) =>
		TimelineRunner.Step(PreviewWorld.Timeline, PreviewWorld, PreviewActorRuntimes, ticksToAdvance);

	public void AdvanceTo(int endTick)
	{
		var ticksToAdvance = endTick - PreviewWorld.Timeline.Clock.Current;
		if (ticksToAdvance <= 0)
			return;

		foreach (var _ in StepPreview(ticksToAdvance)) { }
	}

	public bool TryUndoLast()
	{
		if (_actions.Count == 0)
			return false;

		PopUndoGroup();
		Reevaluate();
		return true;
	}

	public Plan Commit() => new(_actions.ToList());

	public IEnumerable<SearchFrame<TWorld, TRuntime>> Search<TEffect>(
		string actorId,
		IReadOnlyList<IActionDef<IAction, TWorld, TRuntime, TEffect>> actionDefs,
		Func<TWorld, ActorRuntimes<TRuntime>, string, SearchVisitState> visitState)
		where TEffect : IEffect<TWorld, TRuntime>
	{
		var fork = Fork();
		var startDepth = fork._actions.Count;
		var visited = new Dictionary<object, int[]>();

		foreach (var frame in SearchDfs(
			fork,
			actorId,
			actionDefs,
			startDepth,
			0,
			visited,
			visitState))
			yield return frame;
	}

	public void Reevaluate()
	{
		PreviewWorld = _anchorWorld.Fork();
		PreviewActorRuntimes = _anchorActorRuntimes.Fork();

		foreach (var action in _actions)
			ExecutionHelper.Apply(action, PreviewWorld, PreviewActorRuntimes.For(action));
	}

	private Simulation<TWorld, TRuntime> Fork()
	{
		var fork = new Simulation<TWorld, TRuntime>(_anchorWorld, _anchorActorRuntimes)
		{
			_anchorTick = _anchorTick,
			WorldVersion = WorldVersion,
			_nextUndoGroup = _nextUndoGroup,
		};
		fork._actions.AddRange(_actions);
		fork.Reevaluate();
		return fork;
	}

	private int CaptureSearchCheckpoint() => _actions.Count;

	private void RestoreSearchCheckpoint(int actionCount)
	{
		while (_actions.Count > actionCount)
			_actions.RemoveAt(_actions.Count - 1);

		// TODO(search): backtrack rebuilds preview via full Reevaluate (fork+replay).
		// Effect undos would avoid that but are costly to implement; this is the main
		// algorithm pain point once search branching grows.
		Reevaluate();
	}

	private const int MaxSearchDepth = 12;
	private const int HardAbortSearchDepth = 64;

	private static IEnumerable<SearchFrame<TWorld, TRuntime>> SearchDfs<TEffect>(
		Simulation<TWorld, TRuntime> fork,
		string actorId,
		IReadOnlyList<IActionDef<IAction, TWorld, TRuntime, TEffect>> actionDefs,
		int startDepth,
		int depth,
		Dictionary<object, int[]> visited,
		Func<TWorld, ActorRuntimes<TRuntime>, string, SearchVisitState> visitState)
		where TEffect : IEffect<TWorld, TRuntime>
	{
		if (depth > MaxSearchDepth || depth >= HardAbortSearchDepth)
			yield break;

		if (ShouldPruneVisit(visited, visitState, fork, actorId))
			yield break;

		yield return new SearchFrame<TWorld, TRuntime>(
			fork.PreviewWorld.Fork(),
			fork.PreviewActorRuntimes.Fork(),
			fork.Actions.ToList(),
			fork.Actions.Count - startDepth);

		foreach (var def in actionDefs)
		{
			var runtime = fork.PreviewActorRuntimes.For(actorId);
			var candidates = def.Discover(fork.PreviewWorld, runtime, actorId).ToList();

			foreach (var candidate in candidates)
			{
				var checkpoint = fork.CaptureSearchCheckpoint();
				if (!fork.TryEnqueue(candidate))
					continue;

				foreach (var frame in SearchDfs(
					fork,
					actorId,
					actionDefs,
					startDepth,
					depth + 1,
					visited,
					visitState))
					yield return frame;

				fork.RestoreSearchCheckpoint(checkpoint);
			}
		}
	}

	private static bool ShouldPruneVisit(
		Dictionary<object, int[]> visited,
		Func<TWorld, ActorRuntimes<TRuntime>, string, SearchVisitState> visitState,
		Simulation<TWorld, TRuntime> fork,
		string actorId)
	{
		var visit = visitState(fork.PreviewWorld, fork.PreviewActorRuntimes, actorId);
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

	private void PopUndoGroup()
	{
		var last = _actions[^1];
		if (last.UndoGroup is not int group)
		{
			_actions.RemoveAt(_actions.Count - 1);
			return;
		}

		while (_actions.Count > 0 && _actions[^1].UndoGroup == group)
			_actions.RemoveAt(_actions.Count - 1);
	}

}
