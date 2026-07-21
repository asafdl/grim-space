using GrimSpace.Core.Actions;
using GrimSpace.Battle.Slices;

namespace GrimSpace.Battle.Effects;

public sealed class ScheduleActionEffect(int delayTicks, IAction action) : IEffect<BattleSlices>
{
	public void Apply(BattleSlices slices) => slices.Timeline.Schedule(delayTicks, action);
}
