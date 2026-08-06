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

	public IReadOnlyList<IAction> Commit(params IAction[] actions)
	{
		foreach (var action in actions)
			ExecutionHelper.Apply(action, World, ActorRuntimes.For(action));

		World.Timeline.Record(actions);
		BumpWorldVersion();
		return actions;
	}

	public void Schedule(int delayTicks, params IAction[] actions)
	{
		World.Timeline.Schedule(delayTicks, actions);
		BumpWorldVersion();
	}

	public IReadOnlyList<IAction> AdvanceTick()
	{
		World.Timeline.Clock.Next();
		var pending = World.Timeline.TakePending();
		return pending.Count == 0 ? [] : Commit([..pending]);
	}

	public IReadOnlyList<TimelineBatch> History(int? tick = null) =>
		World.Timeline.History(tick);

	public IReadOnlyDictionary<string, IReadOnlyList<IAction>> HistoryByActor(int? tick = null) =>
		World.Timeline.HistoryByActor(tick);

	private void BumpWorldVersion() => WorldVersion++;
}
