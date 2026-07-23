using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Engine;

internal static class TimelineRunner
{
	public static IEnumerable<TickResult> Step<TWorld, TRuntime>(
		Timeline timeline,
		TWorld world,
		ActorRuntimes<TRuntime> runtimes,
		int ticksToAdvance)
		where TWorld : IWorld<TWorld>
		where TRuntime : IRuntimeContext<TRuntime>, new()
	{
		var startTick = timeline.Clock.Current;
		var endTick = startTick + ticksToAdvance;

		for (var tick = startTick; tick <= endTick; tick++)
		{
			timeline.Clock.Set(tick);
			var applied = new List<IAction>();

			while (timeline.At(tick).TryDequeue(out var action) && action is not null)
			{
				ExecutionHelper.Apply(action, world, runtimes.For(action));
				applied.Add(action);
			}

			yield return new TickResult(tick, applied);
		}
	}
}
