using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class ScheduleWorldEventActionEffect(
	int delayTicks,
	IAction action) : IEffect<StarMap, ActorRuntime>
{
	private int _scheduledTick;

	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		_scheduledTick = world.Timeline.Clock.Current + delayTicks;
		world.Timeline.Schedule(delayTicks, action);
		return [];
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId) =>
		world.Timeline.CancelPending(_scheduledTick, action);
}
