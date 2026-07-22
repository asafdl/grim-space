using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Engine;

/// <summary>
/// Stateful planning workspace: anchor world, preview fork, runtime, and action queue.
/// </summary>
public class Simulation<TWorld, TRuntime>
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext<TRuntime>
{
	private readonly List<IAction> _actions = [];
	private readonly TWorld _anchorWorld;
	private readonly TRuntime _anchorRuntime;
	private int _anchorTick;
	private int _nextUndoGroup;

	public Simulation(TWorld anchorWorld, TRuntime anchorRuntime)
	{
		_anchorWorld = anchorWorld;
		_anchorRuntime = anchorRuntime;
	}

	public TWorld PreviewWorld { get; private set; } = default!;

	public TRuntime PreviewRuntime { get; private set; } = default!;

	public IReadOnlyList<IAction> Actions => _actions;

	public int AnchorTick => _anchorTick;

	public TWorld AnchorWorld => _anchorWorld;

	public TRuntime AnchorRuntime => _anchorRuntime;

	public void Begin(int anchorTick)
	{
		_anchorTick = anchorTick;
		_actions.Clear();
		_nextUndoGroup = 0;
		Reevaluate();
	}

	public int AllocateUndoGroup() => ++_nextUndoGroup;

	public bool TryEnqueue(IAction action)
	{
		Reevaluate();
		if (action is not IAction<TWorld, TRuntime> typed)
			return false;

		if (!typed.IsLegal(PreviewWorld, PreviewRuntime))
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

		foreach (var action in _actions)
		{
			if (action is not IAction<TWorld, TRuntime> typed)
				continue;

			typed.Apply(PreviewWorld, PreviewRuntime);
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
