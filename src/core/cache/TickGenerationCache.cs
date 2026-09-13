namespace GrimSpace.Core.Cache;

internal sealed class TickGenerationCache<TKey, TInput, TPrepared, TResource> : IDisposable
	where TKey : notnull
{
	private readonly int _capacity;
	private readonly int _maxPending;
	private readonly int _retentionTicks;
	private readonly Func<TInput, CancellationToken, TPrepared> _prepare;
	private readonly Func<TPrepared, TResource> _finalize;
	private readonly CancellationTokenSource _cancellation = new();
	private readonly Dictionary<TKey, CachedResource> _resources = [];
	private readonly Dictionary<TKey, PendingResource> _pending = [];
	private readonly HashSet<TKey> _failedThisTick = [];

	private int? _currentTick;
	private long _accessOrder;
	private bool _disposed;

	public TickGenerationCache(
		int capacity,
		int maxPending,
		int retentionTicks,
		Func<TInput, CancellationToken, TPrepared> prepare,
		Func<TPrepared, TResource> finalize)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
		ArgumentOutOfRangeException.ThrowIfLessThan(maxPending, 1);
		ArgumentOutOfRangeException.ThrowIfNegative(retentionTicks);
		_capacity = capacity;
		_maxPending = maxPending;
		_retentionTicks = retentionTicks;
		_prepare = prepare;
		_finalize = finalize;
	}

	internal int ResourceCount => _resources.Count;
	internal int PendingCount => _pending.Count;
	internal int CompletedPendingCount => _pending.Values.Count(entry => entry.Task.IsCompleted);

	public bool Request(
		TKey key,
		Func<TInput> input,
		int tick,
		out TResource resource)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		AdvanceTick(tick);

		if (_resources.TryGetValue(key, out var cached))
		{
			cached.LastUsedTick = tick;
			cached.AccessOrder = ++_accessOrder;
			resource = cached.Resource;
			return true;
		}

		if (_pending.TryGetValue(key, out var pending))
		{
			pending.LastUsedTick = tick;
		}
		else if (!_failedThisTick.Contains(key) && _pending.Count < _maxPending)
		{
			var preparedInput = input();
			var task = Task.Run(
				() => _prepare(preparedInput, _cancellation.Token),
				_cancellation.Token);
			_pending.Add(key, new PendingResource(task, tick));
		}

		resource = default!;
		return false;
	}

	public PumpResult Pump(int tick)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		AdvanceTick(tick);
		var completed = _pending
			.Where(pair => pair.Value.Task.IsCompleted)
			.ToArray();
		var finalized = 0;
		var failures = new List<GenerationFailure>();

		foreach (var (key, pending) in completed)
		{
			_pending.Remove(key);
			if (pending.Task.IsCanceled)
			{
				_failedThisTick.Add(key);
				failures.Add(new GenerationFailure(
					key,
					new TaskCanceledException(pending.Task)));
				continue;
			}
			if (pending.Task.Exception is { } error)
			{
				_failedThisTick.Add(key);
				failures.Add(new GenerationFailure(key, error.Flatten()));
				continue;
			}

			var prepared = pending.Task.GetAwaiter().GetResult();
			if (IsExpired(pending.LastUsedTick, tick))
				continue;

			try
			{
				_resources[key] = new CachedResource(
					_finalize(prepared),
					pending.LastUsedTick,
					++_accessOrder);
				finalized++;
			}
			catch (Exception finalizationError)
			{
				_failedThisTick.Add(key);
				failures.Add(new GenerationFailure(key, finalizationError));
			}
		}

		EvictOverCapacity();
		return new PumpResult(completed.Length, finalized, failures);
	}

	public void AdvanceTick(int tick)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (_currentTick is int current && tick < current)
			throw new ArgumentOutOfRangeException(
				nameof(tick),
				tick,
				$"Cache tick cannot move backwards from {current}.");

		if (_currentTick == tick)
			return;

		_currentTick = tick;
		_failedThisTick.Clear();
		foreach (var key in _resources
			.Where(pair => IsExpired(pair.Value.LastUsedTick, tick))
			.Select(pair => pair.Key)
			.ToArray())
		{
			_resources.Remove(key);
		}
	}

	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;
		_cancellation.Cancel();
		foreach (var task in _pending.Values.Select(entry => entry.Task))
		{
			_ = task.ContinueWith(
				faulted => _ = faulted.Exception,
				CancellationToken.None,
				TaskContinuationOptions.OnlyOnFaulted
					| TaskContinuationOptions.ExecuteSynchronously,
				TaskScheduler.Default);
		}
		_cancellation.Dispose();
		_resources.Clear();
		_pending.Clear();
		_failedThisTick.Clear();
	}

	private bool IsExpired(int lastUsedTick, int currentTick) =>
		currentTick - lastUsedTick > _retentionTicks;

	private void EvictOverCapacity()
	{
		while (_resources.Count > _capacity)
		{
			var oldest = _resources.MinBy(pair =>
				(pair.Value.LastUsedTick, pair.Value.AccessOrder));
			_resources.Remove(oldest.Key);
		}
	}

	private sealed class CachedResource(
		TResource resource,
		int lastUsedTick,
		long accessOrder)
	{
		public TResource Resource { get; } = resource;
		public int LastUsedTick { get; set; } = lastUsedTick;
		public long AccessOrder { get; set; } = accessOrder;
	}

	private sealed class PendingResource(Task<TPrepared> task, int lastUsedTick)
	{
		public Task<TPrepared> Task { get; } = task;
		public int LastUsedTick { get; set; } = lastUsedTick;
	}

	internal sealed record GenerationFailure(TKey Key, Exception Error);
	internal sealed record PumpResult(
		int ProcessedCount,
		int FinalizedCount,
		IReadOnlyList<GenerationFailure> Failures);
}
