namespace GrimSpace.Core;

public sealed class PriorityLinkedList<T>
{
	private readonly LinkedList<T> _list = new();
	private readonly Func<T, int> _prioritySelector;

	public PriorityLinkedList(Func<T, int> prioritySelector) =>
		_prioritySelector = prioritySelector ?? throw new ArgumentNullException(nameof(prioritySelector));

	public LinkedListNode<T>? First => _list.First;

	public LinkedListNode<T> Add(T item)
	{
		var priority = _prioritySelector(item);
		for (var node = _list.First; node is not null; node = node.Next)
		{
			if (_prioritySelector(node.Value) > priority)
				return _list.AddBefore(node, item);
		}

		return _list.AddLast(item);
	}

	public void Remove(LinkedListNode<T> node) => _list.Remove(node);
}
