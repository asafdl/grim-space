using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Engine;

/// <summary>
/// Stateful simulation workspace: anchor world, preview fork, per-actor runtimes, and action queue.
/// </summary>
public class Simulation<TWorld, TRuntime>
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext<TRuntime>, new()
{
	private readonly List<IAction> _actions = [];
	private readonly List<int?> _undoGroups = [];
	private readonly TWorld _anchorWorld;
	private readonly ActorRuntimes<TRuntime> _anchorActorRuntimes;
	private int _anchorTick;
	private int _nextUndoGroup;
	private InvariantStatus _invariantStatus = InvariantStatus.Ok;

	public Simulation(TWorld anchorWorld, ActorRuntimes<TRuntime> anchorActorRuntimes)
	{
		_anchorWorld = anchorWorld;
		_anchorActorRuntimes = anchorActorRuntimes;
	}

	public TRuntime RuntimeFor(string actorId) => Runtimes.For(actorId);

	public TActorState StateOf<TActorState>(string actorId) where TActorState : notnull =>
		((IActorStateWorld<TActorState, TWorld>)World).StateOf(actorId);

	internal TWorld World { get; private set; } = default!;

	internal ActorRuntimes<TRuntime> Runtimes { get; private set; } = default!;

	public IReadOnlyList<IAction> Actions => _actions;

	public IReadOnlyList<int?> UndoGroups => _undoGroups;

	public int AnchorTick => _anchorTick;

	public int WorldVersion { get; private set; }

	public bool IsStale(int currentWorldVersion) => WorldVersion != currentWorldVersion;

	public void Begin(int anchorTick, int worldVersion)
	{
		_anchorTick = anchorTick;
		WorldVersion = worldVersion;
		_actions.Clear();
		_undoGroups.Clear();
		_nextUndoGroup = 0;
		_invariantStatus = InvariantStatus.Ok;
		Reevaluate();
	}

	public bool TryEnqueue(params IAction[] actions)
	{
		if (actions.Length == 0)
			return true;

		int? undoGroup = actions.Length > 1 ? ++_nextUndoGroup : null;
		var checkpoint = _actions.Count;

		foreach (var action in actions)
		{
			if (action is not IAction<TWorld, TRuntime> typed)
			{
				RestoreEnqueueCheckpoint(checkpoint);
				return false;
			}

			var runtime = Runtimes.For(action);
			if (!typed.Definition.IsLegal(action, World, runtime))
			{
				RestoreEnqueueCheckpoint(checkpoint);
				return false;
			}

			_actions.Add(action);
			_undoGroups.Add(undoGroup);
			ExecutionHelper.Apply(action, World, Runtimes.For(action));

			if (typed.Definition is IActionInvariants<TWorld, TRuntime> invariants)
				_invariantStatus = invariants.EvaluateInvariants(World, Runtimes.For(action), action.ActorId);
		}

		return true;
	}

	private void RestoreEnqueueCheckpoint(int actionCount)
	{
		while (_actions.Count > actionCount)
		{
			_actions.RemoveAt(_actions.Count - 1);
			_undoGroups.RemoveAt(_undoGroups.Count - 1);
		}

		Reevaluate();
	}

	/// <summary>
	/// Projects world state after applying <paramref name="action"/> on top of the current queue.
	/// Returns null when the action is not legal from the current preview state.
	/// </summary>
	public PeekFrame<TWorld, TRuntime>? Peek(IAction action)
	{
		if (action is not IAction<TWorld, TRuntime> typed)
			return null;

		var runtime = Runtimes.For(action);
		if (!typed.Definition.IsLegal(action, World, runtime))
			return null;

		var world = World.Fork();
		var runtimes = Runtimes.Fork();
		ExecutionHelper.Apply(action, world, runtimes.For(action));
		return new PeekFrame<TWorld, TRuntime>(world, runtimes);
	}

	public IEnumerable<TickResult> StepPreview(int ticksToAdvance) =>
		TimelineRunner.Step(World.Timeline, World, Runtimes, ticksToAdvance);

	public void AdvanceTo(int endTick)
	{
		var ticksToAdvance = endTick - World.Timeline.Clock.Current;
		if (ticksToAdvance <= 0)
			return;

		foreach (var _ in StepPreview(ticksToAdvance)) { }
	}

	public int TimelineMaxTick => World.Timeline.MaxTick;

	public IReadOnlyList<IAction> PeekTimeline(int? tick = null)
	{
		var timeline = World.Timeline;
		var peekTick = tick ?? timeline.Clock.Current + 1;
		return timeline.SnapshotAt(peekTick);
	}

	public bool TryUndoLast()
	{
		if (_actions.Count == 0)
			return false;

		PopUndoGroup();
		Reevaluate();
		return true;
	}

	public bool TryCommit(out IReadOnlyList<IAction> actions, out InvariantStatus status)
	{
		status = _invariantStatus;
		if (_invariantStatus != InvariantStatus.Ok)
		{
			actions = null!;
			return false;
		}

		actions = _actions.ToList();
		return true;
	}

	public IEnumerable<SearchFrame<TWorld, TRuntime>> Search<TEffect>(
		string actorId,
		IReadOnlyList<IActionDef<IAction, TWorld, TRuntime, TEffect>> actionDefs,
		Func<Simulation<TWorld, TRuntime>, string, SearchVisitState> visitState)
		where TEffect : IEffect<TWorld, TRuntime> =>
		Search(actorId, actionDefs, new SearchInput<TWorld, TRuntime>(visitState));

	public IEnumerable<SearchFrame<TWorld, TRuntime>> Search<TEffect>(
		string actorId,
		IReadOnlyList<IActionDef<IAction, TWorld, TRuntime, TEffect>> actionDefs,
		SearchInput<TWorld, TRuntime> input)
		where TEffect : IEffect<TWorld, TRuntime>
	{
		var fork = Fork();
		var startDepth = fork._actions.Count;
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

	/// <summary>
	/// Replays committed actions from the anchor up to (but not including) <paramref name="throughExclusive"/>.
	/// </summary>
	public TWorld ReplayWorld(int throughExclusive)
	{
		var world = _anchorWorld.Fork();
		var runtimes = _anchorActorRuntimes.Fork();
		var limit = System.Math.Min(throughExclusive, _actions.Count);
		for (var i = 0; i < limit; i++)
			ExecutionHelper.Apply(_actions[i], world, runtimes.For(_actions[i]));

		return world;
	}

	public void Reevaluate()
	{
		World = _anchorWorld.Fork();
		Runtimes = _anchorActorRuntimes.Fork();

		foreach (var action in _actions)
			ExecutionHelper.Apply(action, World, Runtimes.For(action));

		RefreshInvariantStatus();
	}

	private void RefreshInvariantStatus()
	{
		_invariantStatus = InvariantStatus.Ok;

		for (var i = _actions.Count - 1; i >= 0; i--)
		{
			if (_actions[i] is not IAction<TWorld, TRuntime> typed)
				continue;

			if (typed.Definition is not IActionInvariants<TWorld, TRuntime> invariants)
				continue;

			_invariantStatus = invariants.EvaluateInvariants(
				World,
				Runtimes.For(_actions[i]),
				_actions[i].ActorId);
			return;
		}
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
		fork._undoGroups.AddRange(_undoGroups);
		fork.Reevaluate();
		return fork;
	}

	/// <summary>Fresh branch from the turn anchor with no queued actions.</summary>
	public Simulation<TWorld, TRuntime> BranchAtTurnStart()
	{
		var branch = new Simulation<TWorld, TRuntime>(_anchorWorld, _anchorActorRuntimes)
		{
			_anchorTick = _anchorTick,
			WorldVersion = WorldVersion,
		};
		branch.Begin(_anchorTick, WorldVersion);
		return branch;
	}

	/// <summary>Branch preserving the current queued actions and preview state.</summary>
	public Simulation<TWorld, TRuntime> Branch() => Fork();

	private int CaptureSearchCheckpoint() => _actions.Count;

	private void RestoreSearchCheckpoint(int actionCount)
	{
		while (_actions.Count > actionCount)
		{
			_actions.RemoveAt(_actions.Count - 1);
			_undoGroups.RemoveAt(_undoGroups.Count - 1);
		}

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
		SearchInput<TWorld, TRuntime> input,
		SearchContext? context)
		where TEffect : IEffect<TWorld, TRuntime>
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
				var checkpoint = fork.CaptureSearchCheckpoint();
				if (!fork.TryEnqueue(candidate))
					continue;

				if (fork._invariantStatus == InvariantStatus.Impossible)
				{
					fork.RestoreSearchCheckpoint(checkpoint);
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

				fork.RestoreSearchCheckpoint(checkpoint);
			}
		}
	}

	private static bool ShouldPruneVisit(
		Dictionary<object, int[]> visited,
		Func<Simulation<TWorld, TRuntime>, string, SearchVisitState> visitState,
		Simulation<TWorld, TRuntime> fork,
		string actorId)
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

	private void PopUndoGroup()
	{
		if (_undoGroups[^1] is not int group)
		{
			_actions.RemoveAt(_actions.Count - 1);
			_undoGroups.RemoveAt(_undoGroups.Count - 1);
			return;
		}

		while (_actions.Count > 0 && _undoGroups[^1] == group)
		{
			_actions.RemoveAt(_actions.Count - 1);
			_undoGroups.RemoveAt(_undoGroups.Count - 1);
		}
	}

}
