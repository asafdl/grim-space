using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Contexts;

namespace GrimSpace.Core.Actions.Battle.Effects;

public sealed class ScheduleActionEffect(int delayTicks, IAction action) : IEffect<BattleSlices>
{
	public void Apply(BattleSlices slices) => slices.Timeline.Schedule(delayTicks, action);
}
