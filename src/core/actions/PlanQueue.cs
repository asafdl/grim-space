namespace GrimSpace.Core.Actions;

public sealed class PlanQueue<TAction>
{
	private readonly List<TAction> _actions = [];

	public IReadOnlyList<TAction> Actions => _actions;

	public int Count => _actions.Count;

	public void Clear() => _actions.Clear();

	public bool TryPopLast(out TAction? action)
	{
		if (_actions.Count == 0)
		{
			action = default;
			return false;
		}

		var index = _actions.Count - 1;
		action = _actions[index];
		_actions.RemoveAt(index);
		return true;
	}

	public void ReplaceOrAdd(TAction action, Func<TAction, bool> matcher)
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

	public void Add(TAction action) => _actions.Add(action);

	public int CountOf<T>() where T : TAction =>
		_actions.Count(action => action is T);
}
