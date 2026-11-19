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
		if (context.OrientationFlowsIntoMove(roll.ActorId))
			return ClipPlayback.Instant;

		var state = context.ReplayState.StateOf(roll.ActorId);
		var seconds = context.Seconds(ReplayTiming.OrientationSeconds);
		context.UnitViews[roll.ActorId].AnimateOrientationTo(state, seconds);
		return ClipPlayback.Pause(seconds);
	}
}
