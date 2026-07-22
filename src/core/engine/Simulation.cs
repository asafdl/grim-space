using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Engine;

/// <summary>
/// Stateful planning workspace: anchor world, preview fork, runtime, and action queue.
/// </summary>
public abstract class Simulation<TWorld, TRuntime, TContext, TSlice>
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext<TRuntime>
	where TContext : ActionContext<TSlice>
{
	private readonly List<IAction> _actions = [];
	private TWorld _anchorWorld = default!;
	private TRuntime _anchorRuntime = default!;
	private int _anchorTick;
	private int _nextUndoGroup;

	public TWorld PreviewWorld { get; private set; } = default!;

	public TRuntime PreviewRuntime { get; private set; } = default!;

	public IReadOnlyList<IAction> Actions => _actions;

	public int AnchorTick => _anchorTick;

	public TWorld AnchorWorld => _anchorWorld;

	public TRuntime AnchorRuntime => _anchorRuntime;

	public void Begin(TWorld anchorWorld, TRuntime anchorRuntime, int anchorTick)
	{
		_anchorWorld = anchorWorld;
		_anchorRuntime = anchorRuntime;
		_anchorTick = anchorTick;
		_actions.Clear();
		_nextUndoGroup = 0;
		Reevaluate();
	}

	public int AllocateUndoGroup() => ++_nextUndoGroup;

	public bool TryEnqueue(IAction action)
	{
		Reevaluate();
		var ctx = CreateContext(PreviewWorld, PreviewRuntime, action.OwnerId);
		if (!IsActionLegal(ctx, action))
			return false;

		_actions.Add(action);
		Reevaluate();
		return true;
	}

	public void ForceEnqueue(IAction action) => _actions.Add(action);

	public void Refresh() => Reevaluate();

	public void CopyActionsFrom(IEnumerable<IAction> actions)
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
		PreviewRuntime = _anchorRuntime.Fork();

		foreach (var action in ExpandPlayback(_actions))
		{
			var ctx = CreateContext(PreviewWorld, PreviewRuntime, action.OwnerId);
			ApplyAction(ctx, action);
		}
	}

	public void AdvanceToTick(int tick, Action<IAction> applyScheduled)
	{
		for (var t = _anchorTick + 1; t <= tick; t++)
		{
			PreviewWorld.Timeline.Clock.Set(t);
			while (PreviewWorld.Timeline.At(t).TryDequeue(out var scheduled) && scheduled is IAction action)
				applyScheduled(action);
		}
	}

	protected abstract TContext CreateContext(TWorld world, TRuntime runtime, string ownerId);

	protected abstract bool IsActionLegal(TContext ctx, IAction action);

	protected abstract void ApplyAction(TContext ctx, IAction action);

	protected virtual IReadOnlyList<IAction> ExpandPlayback(IReadOnlyList<IAction> actions) => actions;

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
