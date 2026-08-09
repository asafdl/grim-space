using GrimSpace.Battle.Actions;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Presentation.Replay.Clips;

public sealed class TorpedoActionClip : IReplayClip
{
	public Type ActionType => typeof(TorpedoAction);

	public ClipPlayback Play(IAction action, ReplayClipContext context)
	{
		context.PendingTorpedoMount = ((TorpedoAction)action).Mount;
		return ClipPlayback.Instant;
	}
}
