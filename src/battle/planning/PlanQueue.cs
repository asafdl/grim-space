namespace GrimSpace.Battle.Planning;

public sealed class PlanQueue
{
	private readonly List<PlannedAction> _actions = [];

	public IReadOnlyList<PlannedAction> Actions => _actions;

	public int Count => _actions.Count;

	public void Clear() => _actions.Clear();

	public bool TryPopLast(out PlannedAction? action)
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

	public void ReplaceOrAdd(PlannedAction action, Func<PlannedAction, bool> matcher)
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

	public void Add(PlannedAction action) => _actions.Add(action);

	public int CountOf<T>() where T : PlannedAction =>
		_actions.Count(action => action is T);
}
