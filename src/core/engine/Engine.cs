using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Engine;

internal sealed class Engine<TWorld, TRuntime> : IDisposable
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext<TRuntime>, new()
{
	private readonly TimelineGcOptions _gcOptions;
	private readonly CancellationTokenSource _gcCts = new();
	private readonly Task _gcTask;
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

	public Simulation<TWorld, TRuntime> CreateSimulation()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		var sim = new Simulation<TWorld, TRuntime>(World.Fork(), ActorRuntimes.Fork());
		sim.Begin(Tick, WorldVersion);
		return sim;
	}

	public IReadOnlyList<ITimelineEntry> Commit(params IAction[] actions)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (actions.Length == 0)
			return World.Timeline.History();

		foreach (var action in actions)
		{
			var records = ExecutionHelper.Apply(action, World, ActorRuntimes.For(action));
			World.Timeline.Append([action, ..records]);
		}

		BumpWorldVersion();
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

	private void BumpWorldVersion() => WorldVersion++;
}
