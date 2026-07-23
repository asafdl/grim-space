using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Engine;

public sealed class Engine<TWorld, TRuntime>
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext<TRuntime>, new()
{
	public Engine(TWorld world, ActorRuntimes<TRuntime> actorRuntimes)
	{
		World = world;
		ActorRuntimes = actorRuntimes;
	}

	public TWorld World { get; }

	public ActorRuntimes<TRuntime> ActorRuntimes { get; }

	public int WorldVersion { get; private set; }

	public Simulation<TWorld, TRuntime> CreateSimulation()
	{
		var sim = new Simulation<TWorld, TRuntime>(World.Fork(), ActorRuntimes.Fork());
		sim.Begin(World.Timeline.Clock.Current, WorldVersion);
		return sim;
	}

	/// <summary>
	/// Schedule a simulation's plan onto the live timeline. Stale simulations rebase first
	/// (save actions → refork from live → replay). Returns false if replay fails.
	/// </summary>
	public bool TryScheduleFromSimulation(
		Simulation<TWorld, TRuntime> simulation,
		out Simulation<TWorld, TRuntime> current,
		IReadOnlyList<IAction> actions,
		int delayTicks = 0)
	{
		var resolved = EnsureCurrent(simulation);
		if (resolved is null)
		{
			current = simulation;
			return false;
		}

		current = resolved;
		ScheduleToWorldTimeline(actions, delayTicks);
		return true;
	}

	public void ScheduleToWorldTimeline(Plan plan, int delayTicks = 0) =>
		ScheduleToWorldTimeline(plan.Actions, delayTicks);

	public void ScheduleToWorldTimeline(IReadOnlyList<IAction> actions, int delayTicks = 0)
	{
		var tick = World.Timeline.Clock.Current + delayTicks;
		World.Timeline.At(tick).EnqueueAll(actions);
		BumpWorldVersion();
	}

	public void ScheduleToWorldTimeline(IAction action, int delayTicks = 0)
	{
		var tick = World.Timeline.Clock.Current + delayTicks;
		World.Timeline.At(tick).Enqueue(action);
		BumpWorldVersion();
	}

	public IEnumerable<TickResult> Step(int ticksToAdvance)
	{
		foreach (var tick in TimelineRunner.Step(World.Timeline, World, ActorRuntimes, ticksToAdvance))
			yield return tick;

		BumpWorldVersion();
	}

	private Simulation<TWorld, TRuntime>? EnsureCurrent(Simulation<TWorld, TRuntime> simulation) =>
		simulation.IsStale(WorldVersion) ? Rebase(simulation) : simulation;

	private Simulation<TWorld, TRuntime>? Rebase(Simulation<TWorld, TRuntime> simulation)
	{
		var saved = simulation.Actions.ToList();
		var fresh = CreateSimulation();

		foreach (var action in saved)
		{
			if (!fresh.TryEnqueue(action))
				return null;
		}

		return fresh;
	}

	private void BumpWorldVersion() => WorldVersion++;
}
