using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Engine;

public sealed class Timeline
{
	private readonly Dictionary<int, TimelineBucket> _buckets = new();

	public TickClock Clock { get; } = new();

	public TimelineBucket At(int tick)
	{
		if (!_buckets.TryGetValue(tick, out var bucket))
		{
			bucket = new TimelineBucket();
			_buckets[tick] = bucket;
		}

		return bucket;
	}

	public void Schedule(int delayTicks, IAction action) =>
		At(Clock.Current + delayTicks).EnqueueFirst(action);

	public int MaxTick => _buckets.Count == 0 ? 0 : _buckets.Keys.Max();

	public Timeline Clone()
	{
		var clone = new Timeline();
		clone.Clock.Set(Clock.Current);
		foreach (var (tick, bucket) in _buckets)
			clone.At(tick).EnqueueAll(bucket.Snapshot());

		return clone;
	}

	public IReadOnlyList<IAction> SnapshotAt(int tick) =>
		_buckets.TryGetValue(tick, out var bucket) ? bucket.Snapshot() : [];

	public IEnumerable<(int Tick, IAction Action)> From(int startTick)
	{
		foreach (var tick in _buckets.Keys.Where(t => t >= startTick).OrderBy(t => t))
		{
			foreach (var action in _buckets[tick].Snapshot())
				yield return (tick, action);
		}
	}
}
