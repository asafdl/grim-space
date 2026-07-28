using GrimSpace.Battle.Actions;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Presentation.Replay.Clips;

public sealed class RollClip : IReplayClip
{
	public Type ActionType => typeof(RollAction);

	public ClipPlayback Play(IAction action, ReplayClipContext context)
	{
		var roll = (RollAction)action;
		context.ReplayState.ApplyRoll(roll);
		context.SyncUnit(roll.ActorId);
		return ClipPlayback.Instant;
	}
}
