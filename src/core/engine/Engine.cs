using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Engine;

internal sealed class Engine<TWorld, TRuntime> : IDisposable
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext<TRuntime>, new()
{
	private readonly TimelineGcOptions _gcOptions;
	private readonly CancellationTokenSource _gcCts = new();
	private readonly Task _gcTask;
	private readonly object _subscriptionsSync = new();
	private readonly Dictionary<Type, List<Action<ITimelineEntry>>> _subscriptions = [];
	private bool _disposed;

	public Engine(
		TWorld world,
		ActorRuntimes<TRuntime> actorRuntimes,
		TimelineGcOptions? gcOptions = null)
	{
		World = world;
		ActorRuntimes = actorRuntimes;
		_gcOptions = gcOptions ?? TimelineGcOptions.Default;
		if (World.Timeline.Clock.Current == 0)
			World.Timeline.Clock.Set(1);

		_gcTask = _gcOptions == TimelineGcOptions.Disabled
			? Task.CompletedTask
			: Task.Run(() => RunTimelineGcAsync(_gcCts.Token));
	}

	public TWorld World { get; }

	public ActorRuntimes<TRuntime> ActorRuntimes { get; }

	public int WorldVersion { get; private set; }

	public int Tick => World.Timeline.Clock.Current;

	public IDisposable Subscribe<TEntry>(Action<TEntry> listener)
		where TEntry : ITimelineEntry
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		ArgumentNullException.ThrowIfNull(listener);

		Action<ITimelineEntry> wrapper = entry => listener((TEntry)entry);
		var entryType = typeof(TEntry);
		lock (_subscriptionsSync)
		{
			if (!_subscriptions.TryGetValue(entryType, out var listeners))
			{
				listeners = [];
				_subscriptions.Add(entryType, listeners);
			}

			listeners.Add(wrapper);
		}

		return new Subscription(() => Unsubscribe(entryType, wrapper));
	}

	public Simulation<TWorld, TRuntime> CreateSimulation()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		var sim = new Simulation<TWorld, TRuntime>(World.ForkForSimulation(), ActorRuntimes.Fork());
		sim.Begin(Tick, WorldVersion);
		return sim;
	}

	public IReadOnlyList<ITimelineEntry> Commit(params IAction[] actions)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (actions.Length == 0)
			return World.Timeline.History();

		var committed = new List<ITimelineEntry>();
		foreach (var action in actions)
		{
			var records = ExecutionHelper.Apply(action, World, ActorRuntimes.For(action));
			ITimelineEntry[] entries = [action, ..records];
			World.Timeline.Append(entries);
			committed.AddRange(entries);
		}

		BumpWorldVersion();
		PublishCommitted(committed);
		return World.Timeline.History();
	}

	public void Schedule(int delayTicks, params IAction[] actions)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		World.Timeline.Schedule(delayTicks, actions);
		BumpWorldVersion();
	}

	public IReadOnlyList<ITimelineEntry> AdvanceTick()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		World.Timeline.Clock.Next();
		var pending = World.Timeline.TakePending();
		return pending.Count == 0 ? [] : Commit([..pending]);
	}

	public IReadOnlyList<ITimelineEntry> History(int? tick = null) =>
		World.Timeline.History(tick);

	public IReadOnlyList<TimelineBatch> DrainUntil(int tick) =>
		World.Timeline.DrainUntil(tick);

	public IReadOnlyDictionary<string, IReadOnlyList<IAction>> HistoryByActor(int? tick = null) =>
		World.Timeline.HistoryByActor(tick);

	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;
		lock (_subscriptionsSync)
			_subscriptions.Clear();
		if (_gcOptions == TimelineGcOptions.Disabled)
			return;

		_gcCts.Cancel();
		try
		{
			_gcTask.Wait(TimeSpan.FromSeconds(5));
		}
		catch (AggregateException ex) when (ex.InnerExceptions.All(inner => inner is OperationCanceledException))
		{
		}

		_gcCts.Dispose();
	}

	private async Task RunTimelineGcAsync(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				await Task.Delay(_gcOptions.Interval, cancellationToken);
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				break;
			}

			if (_disposed)
				break;

			World.Timeline.TrimHistory(_gcOptions.RetentionTicks);
		}
	}

	private void PublishCommitted(IReadOnlyList<ITimelineEntry> entries)
	{
		foreach (var entry in entries)
		{
			Action<ITimelineEntry>[] listeners;
			lock (_subscriptionsSync)
			{
				if (!_subscriptions.TryGetValue(entry.GetType(), out var registered))
					continue;

				listeners = [..registered];
			}

			foreach (var listener in listeners)
				listener(entry);
		}
	}

	private void Unsubscribe(Type entryType, Action<ITimelineEntry> listener)
	{
		lock (_subscriptionsSync)
		{
			if (!_subscriptions.TryGetValue(entryType, out var listeners))
				return;

			listeners.Remove(listener);
			if (listeners.Count == 0)
				_subscriptions.Remove(entryType);
		}
	}

	private void BumpWorldVersion() => WorldVersion++;

	private sealed class Subscription(Action unsubscribe) : IDisposable
	{
		private Action? _unsubscribe = unsubscribe;

		public void Dispose() => Interlocked.Exchange(ref _unsubscribe, null)?.Invoke();
	}
}
