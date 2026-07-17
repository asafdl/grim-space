namespace GrimSpace.Core.Actions;

public sealed class PlanQueue<TAction> where TAction : IAction
{
	private readonly List<TAction> _actions = [];

	public IReadOnlyList<TAction> Actions => _actions;

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

	public void Enqueue(TAction action) => _actions.Add(action);
}
