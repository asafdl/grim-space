using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Engine;

/// <summary>
/// Fork-and-replay simulation shell: draft action queue, preview timeline, and batch undo.
/// Domain-specific world/state reset and action application are wired by the caller.
/// </summary>
public sealed class Simulator<TWorld, TState>
{
	private readonly List<IAction> _actions = [];
	private readonly Timeline _previewTimeline = new();
	private int _anchorTick;
	private int _nextUndoGroup;

	public Simulator(Func<TState> createState) => State = createState();

	public TWorld World { get; private set; } = default!;

	public TState State { get; }

	public IReadOnlyList<IAction> Actions => _actions;

	public Timeline PreviewTimeline => _previewTimeline;

	public int AnchorTick => _anchorTick;

	public void SetWorld(TWorld world) => World = world;

	public void Begin(int anchorTick)
	{
		_anchorTick = anchorTick;
		_actions.Clear();
		_nextUndoGroup = 0;
		_previewTimeline.ResetPreviewFork(anchorTick);
	}

	public int AllocateUndoGroup() => ++_nextUndoGroup;

	public void Enqueue(IAction action) => _actions.Add(action);

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
		return true;
	}

	public void AdvanceToTick(int tick, Action<IAction> applyScheduled)
	{
		for (var t = _anchorTick + 1; t <= tick; t++)
		{
			_previewTimeline.Clock.Set(t);
			while (_previewTimeline.At(t).TryDequeue(out var scheduled) && scheduled is not null)
				applyScheduled(scheduled);
		}
	}

	public void ResetPreviewFork()
	{
		_previewTimeline.ResetPreviewFork(_anchorTick);
		_previewTimeline.Clock.Set(_anchorTick);
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
