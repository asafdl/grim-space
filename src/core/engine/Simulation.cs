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
	private readonly List<IReadOnlyList<IEffect<TWorld, TRuntime>>> _appliedEffects = [];
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

	public InvariantStatus InvariantStatus => _invariantStatus;

	public int AnchorTick => _anchorTick;

	public int WorldVersion { get; private set; }

	public bool IsStale(int currentWorldVersion) => WorldVersion != currentWorldVersion;

	public void Begin(int anchorTick, int worldVersion)
	{
		_anchorTick = anchorTick;
		WorldVersion = worldVersion;
		_actions.Clear();
		_undoGroups.Clear();
		_appliedEffects.Clear();
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
				Dequeue(checkpoint);
				return false;
			}

			var runtime = Runtimes.For(action);
			if (!typed.Definition.IsLegal(action, World, runtime))
			{
				Dequeue(checkpoint);
				return false;
			}

			_actions.Add(action);
			_undoGroups.Add(undoGroup);
			var effects = ExecutionHelper.ApplyAndResolve(action, World, runtime);
			_appliedEffects.Add(effects);

			if (typed.Definition is IActionInvariants<TWorld, TRuntime> invariants)
				_invariantStatus = invariants.EvaluateInvariants(World, runtime, action.ActorId);
		}

		return true;
	}

	/// <summary>
	/// Pops queued actions and undoes their stored effects.
	/// With <paramref name="depth"/>, pops individual actions until the queue reaches that count.
	/// Without depth, pops one undo group (or a single action when not grouped).
	/// </summary>
	public void Dequeue(int? depth = null)
	{
		if (depth is null)
			DequeueUndoGroup();
		else
			while (_actions.Count > depth)
				DequeueSingleAction();

		RefreshInvariantStatus();
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

		Dequeue();
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
		_appliedEffects.Clear();

		foreach (var action in _actions)
		{
			var runtime = Runtimes.For(action);
			var effects = ExecutionHelper.ApplyAndResolve(action, World, runtime);
			_appliedEffects.Add(effects);
		}

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

	/// <summary>Fork preserving the current queued actions and preview state.</summary>
	public Simulation<TWorld, TRuntime> Fork()
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

	/// <summary>Fresh fork from the anchor with no queued actions.</summary>
	public Simulation<TWorld, TRuntime> ForkFromAnchor()
	{
		var fork = new Simulation<TWorld, TRuntime>(_anchorWorld, _anchorActorRuntimes)
		{
			_anchorTick = _anchorTick,
			WorldVersion = WorldVersion,
		};
		fork.Begin(_anchorTick, WorldVersion);
		return fork;
	}

	private void DequeueSingleAction()
	{
		var action = _actions[^1];
		var effects = _appliedEffects[^1];
		_actions.RemoveAt(_actions.Count - 1);
		_undoGroups.RemoveAt(_undoGroups.Count - 1);
		_appliedEffects.RemoveAt(_appliedEffects.Count - 1);
		ExecutionHelper.UndoEffects(effects, action, World, Runtimes.For(action));
	}

	private void DequeueUndoGroup()
	{
		if (_undoGroups[^1] is not int group)
		{
			DequeueSingleAction();
			return;
		}

		while (_actions.Count > 0 && _undoGroups[^1] == group)
			DequeueSingleAction();
	}
}
