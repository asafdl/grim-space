using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class ScheduleActionEffect(int delayTicks, IAction action) : IEffect<BattleWorld, ActorRuntime>
{
	private int _scheduledTick;

	public void Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		_scheduledTick = world.Timeline.Clock.Current + delayTicks;
		world.Timeline.Schedule(delayTicks, action);
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId) =>
		world.Timeline.At(_scheduledTick).Remove(action);
}
