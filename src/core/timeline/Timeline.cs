using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Engine;

public sealed class Timeline
{
	private readonly Dictionary<int, List<ITimelineEntry>> _history = new();
	private readonly Dictionary<int, List<IAction>> _pending = new();

	public TickClock Clock { get; } = new();

	public void Append(params ITimelineEntry[] entries)
	{
		if (entries.Length == 0)
			return;

		HistoryList().AddRange(entries);
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

	public IReadOnlyList<ITimelineEntry> History(int? tick = null) =>
		_history.TryGetValue(tick ?? Clock.Current, out var entries) ? entries : [];

	public IReadOnlyList<TimelineBatch> DrainUntil(int tick)
	{
		var batches = new List<TimelineBatch>();
		foreach (var key in _history.Keys.Where(key => key <= tick).OrderBy(key => key).ToArray())
		{
			if (!_history.Remove(key, out var entries) || entries.Count == 0)
				continue;

			batches.Add(new TimelineBatch(key, entries));
		}

		return batches;
	}

	public IReadOnlyDictionary<string, IReadOnlyList<IAction>> HistoryByActor(int? tick = null)
	{
		var result = new Dictionary<string, List<IAction>>(StringComparer.Ordinal);
		foreach (var entry in History(tick))
		{
			if (entry is not IAction action)
				continue;

			if (!result.TryGetValue(action.ActorId, out var list))
			{
				list = [];
				result[action.ActorId] = list;
			}

			list.Add(action);
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
		foreach (var (tick, entries) in _history)
			clone._history[tick] = [..entries];

		foreach (var (tick, actions) in _pending)
			clone._pending[tick] = [..actions];

		return clone;
	}

	private List<ITimelineEntry> HistoryList()
	{
		var tick = Clock.Current;
		if (!_history.TryGetValue(tick, out var entries))
		{
			entries = [];
			_history[tick] = entries;
		}

		return entries;
	}
}
