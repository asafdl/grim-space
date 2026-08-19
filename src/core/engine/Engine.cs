using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Engine;

internal sealed class Engine<TWorld, TRuntime>
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext<TRuntime>, new()
{
	public Engine(TWorld world, ActorRuntimes<TRuntime> actorRuntimes)
	{
		World = world;
		ActorRuntimes = actorRuntimes;
		if (World.Timeline.Clock.Current == 0)
			World.Timeline.Clock.Set(1);
	}

	public TWorld World { get; }

	public ActorRuntimes<TRuntime> ActorRuntimes { get; }

	public int WorldVersion { get; private set; }

	public int Tick => World.Timeline.Clock.Current;

	public Simulation<TWorld, TRuntime> CreateSimulation()
	{
		var sim = new Simulation<TWorld, TRuntime>(World.Fork(), ActorRuntimes.Fork());
		sim.Begin(Tick, WorldVersion);
		return sim;
	}

	public IReadOnlyList<ITimelineEntry> Commit(params IAction[] actions)
	{
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
		World.Timeline.Schedule(delayTicks, actions);
		BumpWorldVersion();
	}

	public IReadOnlyList<ITimelineEntry> AdvanceTick()
	{
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

	private void BumpWorldVersion() => WorldVersion++;
}
