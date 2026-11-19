using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Presentation.Replay.Clips;

public sealed class HeadingTurnClip : IReplayClip
{
	public Type ActionType => typeof(HeadingTurnAction);

	public ClipPlayback Play(IAction action, ReplayClipContext context)
	{
		var heading = (HeadingTurnAction)action;
		context.ReplayState.ApplyHeadingTurn(heading);
		if (context.OrientationFlowsIntoMove(heading.ActorId))
			return ClipPlayback.Instant;

		var state = context.ReplayState.StateOf(heading.ActorId);
		var seconds = context.Seconds(ReplayTiming.OrientationSeconds);
		context.UnitViews[heading.ActorId].AnimateOrientationTo(
			state,
			seconds,
			emitTrail: false);
		return ClipPlayback.Pause(seconds);
	}
}
