using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Presentation.Replay.Clips;

public sealed class RollClip : IReplayClip
{
	public Type ActionType => typeof(RollAction);

	public ClipPlayback Play(IAction action, ReplayClipContext context)
	{
		var roll = (RollAction)action;
		context.ReplayState.ApplyRoll(roll);
		var state = context.ReplayState.StateOf(roll.ActorId);
		context.UnitViews[roll.ActorId].AnimateOrientationTo(state, ReplayTiming.OrientationSeconds);
		return ClipPlayback.Pause(ReplayTiming.OrientationSeconds);
	}
}
