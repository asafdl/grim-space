using GrimSpace.Battle.Actions;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Presentation.Replay.Clips;

public sealed class HeadingTurnClip : IReplayClip
{
	public Type ActionType => typeof(HeadingTurnAction);

	public ClipPlayback Play(IAction action, ReplayClipContext context)
	{
		var heading = (HeadingTurnAction)action;
		context.ReplayState.ApplyHeadingTurn(heading);
		context.SyncUnit(heading.ActorId);
		return ClipPlayback.Instant;
	}
}
