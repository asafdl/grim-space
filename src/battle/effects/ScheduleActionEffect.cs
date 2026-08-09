using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class ScheduleActionEffect(int delayTicks, IAction action) : IEffect<BattleWorld, ActorRuntime>
{
	private int _scheduledTick;
	private bool _scheduled;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		_scheduledTick = world.Timeline.Clock.Current + delayTicks;
		world.Timeline.Schedule(delayTicks, action);
		_scheduled = true;
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		if (!_scheduled)
			return;

		world.Timeline.CancelPending(_scheduledTick, action);
		_scheduled = false;
	}
}
