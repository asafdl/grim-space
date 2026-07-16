using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Turn;

public sealed class GlobalActionQueue
{
	private readonly Queue<IAction> _actions = new();

	public bool IsEmpty => _actions.Count == 0;

	public void EnqueueAll(IEnumerable<IAction> actions)
	{
		foreach (var action in actions)
			_actions.Enqueue(action);
	}

	public bool TryDequeue(out IAction? action)
	{
		if (_actions.Count == 0)
		{
			action = null;
			return false;
		}

		action = _actions.Dequeue();
		return true;
	}

	public IReadOnlyList<IAction> Snapshot() => _actions.ToList();
}
