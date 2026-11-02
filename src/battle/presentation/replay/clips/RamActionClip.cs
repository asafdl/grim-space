using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Presentation.Replay.Clips;

public sealed class RamActionClip : IReplayClip
{
	public Type ActionType => typeof(RamAction);

	public ClipPlayback Play(IAction action, ReplayClipContext context)
	{
		var ram = (RamAction)action;
		var state = context.ReplayState.StateOf(ram.ActorId);
		if (context.FollowingAreaDamage(ram.ActorId, EImpactCause.RamImpact) is not { Cells.Count: > 0 } area)
			return ClipPlayback.Instant;

		var impact = area.Cells.First();
		state.Position = impact - state.Fore;
		context.UnitViews[ram.ActorId].AnimateRam(state);
		return ClipPlayback.Pause(RamChargeEffect.DurationSeconds);
	}
}
