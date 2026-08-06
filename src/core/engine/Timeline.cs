using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Engine;

public readonly record struct TimelineBatch(string ActorId, IReadOnlyList<IAction> Actions);

public sealed class Timeline
{
	private readonly Dictionary<int, List<TimelineBatch>> _history = new();
	private readonly Dictionary<int, List<IAction>> _pending = new();

	public TickClock Clock { get; } = new();

	public void Record(IReadOnlyList<IAction> actions, string? actorId = null)
	{
		if (actions.Count == 0)
			return;

		if (actorId is not null)
		{
			RecordBatch(actorId, actions);
			return;
		}

		string? current = null;
		var batch = new List<IAction>();
		foreach (var action in actions)
		{
			if (current is not null && action.ActorId != current)
			{
				RecordBatch(current, batch);
				batch = [];
			}

			current = action.ActorId;
			batch.Add(action);
		}

		if (current is not null)
			RecordBatch(current, batch);
	}

	public void Schedule(int delayTicks, params IAction[] actions)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(delayTicks);
		if (actions.Length == 0)
			return;

		var tick = Clock.Current + delayTicks;
		if (!_pending.TryGetValue(tick, out var list))
		{
			list = [];
			_pending[tick] = list;
		}

		list.AddRange(actions);
	}

	public IReadOnlyList<IAction> TakePending(int? tick = null)
	{
		if (!_pending.Remove(tick ?? Clock.Current, out var list))
			return [];

		return list;
	}

	public bool CancelPending(int tick, IAction action)
	{
		if (!_pending.TryGetValue(tick, out var list))
			return false;

		var removed = list.Remove(action);
		if (list.Count == 0)
			_pending.Remove(tick);

		return removed;
	}

	public IReadOnlyList<TimelineBatch> History(int? tick = null) =>
		_history.TryGetValue(tick ?? Clock.Current, out var batches) ? batches : [];

	public IReadOnlyDictionary<string, IReadOnlyList<IAction>> HistoryByActor(int? tick = null)
	{
		var result = new Dictionary<string, List<IAction>>(StringComparer.Ordinal);
		foreach (var batch in History(tick))
		{
			if (!result.TryGetValue(batch.ActorId, out var list))
			{
				list = [];
				result[batch.ActorId] = list;
			}

			list.AddRange(batch.Actions);
		}

		return result.ToDictionary(
			pair => pair.Key,
			pair => (IReadOnlyList<IAction>)pair.Value,
			StringComparer.Ordinal);
	}

	public Timeline Clone()
	{
		var clone = new Timeline();
		clone.Clock.Set(Clock.Current);
		foreach (var (tick, batches) in _history)
		{
			clone._history[tick] = batches
				.Select(batch => new TimelineBatch(batch.ActorId, batch.Actions.ToList()))
				.ToList();
		}

		foreach (var (tick, actions) in _pending)
			clone._pending[tick] = [..actions];

		return clone;
	}

	private void RecordBatch(string actorId, IReadOnlyList<IAction> actions)
	{
		var tick = Clock.Current;
		if (!_history.TryGetValue(tick, out var batches))
		{
			batches = [];
			_history[tick] = batches;
		}

		batches.Add(new TimelineBatch(actorId, actions.ToList()));
	}
}
