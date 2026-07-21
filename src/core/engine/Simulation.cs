using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Engine;

/// <summary>
/// Stateful planning workspace: anchor world, preview fork, runtime, and action queue.
/// </summary>
public class Simulation<TWorld, TRuntime, TContext, TSlice, TAction>
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext, new()
	where TContext : ActionContext<TSlice>
	where TAction : class, IAction<TContext, TSlice>
{
	private readonly List<TAction> _actions = [];
	private readonly Func<TWorld, TRuntime, string, TContext> _createContext;
	private readonly Func<IReadOnlyList<TAction>, IReadOnlyList<TAction>> _expandPlayback;
	private TWorld _anchorWorld = default!;
	private TRuntime _previewRuntime = new();
	private int _anchorTick;
	private int _nextUndoGroup;

	public Simulation(
		Func<TWorld, TRuntime, string, TContext> createContext,
		Func<IReadOnlyList<TAction>, IReadOnlyList<TAction>>? expandPlayback = null)
	{
		_createContext = createContext;
		_expandPlayback = expandPlayback ?? (actions => actions);
	}

	public TWorld PreviewWorld { get; private set; } = default!;

	public TRuntime PreviewRuntime => _previewRuntime;

	public IReadOnlyList<TAction> Actions => _actions;

	public int AnchorTick => _anchorTick;

	public void Begin(TWorld anchorWorld, int anchorTick)
	{
		_anchorWorld = anchorWorld;
		_anchorTick = anchorTick;
		_actions.Clear();
		_nextUndoGroup = 0;
		Reevaluate();
	}

	public int AllocateUndoGroup() => ++_nextUndoGroup;

	public bool TryEnqueue(TAction action)
	{
		Reevaluate();
		var ctx = _createContext(PreviewWorld, _previewRuntime, action.OwnerId);
		if (!action.IsLegal(ctx))
			return false;

		_actions.Add(action);
		Reevaluate();
		return true;
	}

	public void ForceEnqueue(TAction action) => _actions.Add(action);

	public void Refresh() => Reevaluate();

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
		Reevaluate();
		return true;
	}

	public void Reevaluate()
	{
		PreviewWorld = _anchorWorld.Fork();
		_previewRuntime.Reset();

		foreach (var action in _expandPlayback(_actions))
		{
			var ctx = _createContext(PreviewWorld, _previewRuntime, action.OwnerId);
			SimulationRunner<TContext, TSlice, TAction>.Step(ctx, action);
		}
	}

	public void AdvanceToTick(int tick, Action<TAction> applyScheduled)
	{
		for (var t = _anchorTick + 1; t <= tick; t++)
		{
			PreviewWorld.Timeline.Clock.Set(t);
			while (PreviewWorld.Timeline.At(t).TryDequeue(out var scheduled) && scheduled is TAction action)
				applyScheduled(action);
		}
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
