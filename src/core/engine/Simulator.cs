using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Engine;

/// <summary>
/// Planning simulator: queued actions, legality on enqueue, replay via Simulate.
/// </summary>
public sealed class Simulator<TWorld, TState, TSlice, TAction>
	where TWorld : ISimulationFork<TWorld>
	where TState : ISimulationState
	where TAction : class, IAction<TWorld, TState, TSlice>
{
	private readonly List<TAction> _actions = [];
	private readonly Timeline _timeline = new();
	private readonly Func<TWorld, TState, Timeline, string, TSlice> _createSlices;
	private readonly Func<IReadOnlyList<TAction>, IReadOnlyList<TAction>> _expandPlayback;
	private TWorld _anchorWorld;
	private int _anchorTick;
	private int _nextUndoGroup;

	public Simulator(
		TWorld world,
		TState state,
		Func<TWorld, TState, Timeline, string, TSlice> createSlices,
		Func<IReadOnlyList<TAction>, IReadOnlyList<TAction>>? expandPlayback = null)
	{
		_anchorWorld = world.Fork();
		World = world;
		State = state;
		_createSlices = createSlices;
		_expandPlayback = expandPlayback ?? (actions => actions);
	}

	public TWorld World { get; private set; }

	public TState State { get; }

	public IReadOnlyList<TAction> Actions => _actions;

	public Timeline Timeline => _timeline;

	public int AnchorTick => _anchorTick;

	public void Begin(int anchorTick)
	{
		_anchorTick = anchorTick;
		_actions.Clear();
		_nextUndoGroup = 0;
		_timeline.ResetPreviewFork(anchorTick);
		Replay();
	}

	public int AllocateUndoGroup() => ++_nextUndoGroup;

	public bool TryEnqueue(TAction action)
	{
		if (!action.IsLegal(World, State, _actions.Cast<IAction>()))
			return false;

		_actions.Add(action);
		Replay();
		return true;
	}

	public void ForceEnqueue(TAction action) => _actions.Add(action);

	public void Refresh() => Replay();

	public void CopyActionsFrom(IEnumerable<TAction> actions)
	{
		_actions.Clear();
		foreach (var action in actions)
			_actions.Add(action);
	}

	public bool TryUndoLast()
	{
		if (_actions.Count == 0)
			return false;

		PopUndoGroup();
		Replay();
		return true;
	}

	public void Simulate()
	{
		_timeline.ResetPreviewFork(_anchorTick);
		_timeline.Clock.Set(_anchorTick);

		var applied = new List<IAction>();
		foreach (var action in _expandPlayback(_actions))
		{
			var slices = _createSlices(World, State, _timeline, action.OwnerId);
			foreach (var effect in action.Resolve(World, State, applied))
				effect.Apply(slices);

			applied.Add(action);
		}
	}

	public void AdvanceToTick(int tick, Action<TAction> applyScheduled)
	{
		for (var t = _anchorTick + 1; t <= tick; t++)
		{
			_timeline.Clock.Set(t);
			while (_timeline.At(t).TryDequeue(out var scheduled) && scheduled is TAction action)
				applyScheduled(action);
		}
	}

	private void Replay()
	{
		World = _anchorWorld.Fork();
		State.Clear();
		Simulate();
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
