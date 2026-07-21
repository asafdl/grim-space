using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Engine;

public sealed class ActionQueue
{
	private readonly LinkedList<IAction> _actions = new();

	public void Enqueue(IAction action) => _actions.AddLast(action);

	public void EnqueueFirst(IAction action) => _actions.AddFirst(action);

	public void EnqueueAll(IEnumerable<IAction> actions)
	{
		foreach (var action in actions)
			_actions.AddLast(action);
	}

	public bool TryDequeue(out IAction? action)
	{
		if (_actions.Count == 0)
		{
			action = null;
			return false;
		}

		action = _actions.First!.Value;
		_actions.RemoveFirst();
		return true;
	}

	public IReadOnlyList<IAction> Snapshot() => _actions.ToList();

	public void Clear() => _actions.Clear();
}
