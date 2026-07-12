namespace GrimSpace.Battle.Actions;

public sealed class PlanQueue
{
	private readonly List<IAction> _actions = [];

	public IReadOnlyList<IAction> Actions => _actions;

	public int Count => _actions.Count;

	public void Clear() => _actions.Clear();

	public bool TryPopLast(out IAction? action)
	{
		if (_actions.Count == 0)
		{
			action = null;
			return false;
		}

		var index = _actions.Count - 1;
		action = _actions[index];
		_actions.RemoveAt(index);
		return true;
	}

	public void ReplaceOrAdd(IAction action, Func<IAction, bool> matcher)
	{
		for (var i = 0; i < _actions.Count; i++)
		{
			if (!matcher(_actions[i]))
				continue;

			_actions[i] = action;
			return;
		}

		_actions.Add(action);
	}

	public void Add(IAction action) => _actions.Add(action);

	public int CountOf<T>() where T : IAction =>
		_actions.Count(action => action is T);
}
