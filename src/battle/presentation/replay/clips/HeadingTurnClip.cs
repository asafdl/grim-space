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
		var state = context.ReplayState.StateOf(heading.ActorId);
		context.UnitViews[heading.ActorId].AnimateOrientationTo(state, ReplayTiming.OrientationSeconds);
		return ClipPlayback.Pause(ReplayTiming.OrientationSeconds);
	}
}
