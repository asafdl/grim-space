using GrimSpace.Battle.Actions;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Presentation.Replay.Clips;

public sealed class VoidBombActionClip : IReplayClip
{
	public Type ActionType => typeof(VoidBombAction);

	public ClipPlayback Play(IAction action, ReplayClipContext context)
	{
		context.PendingVoidBombMountedOn = ((VoidBombAction)action).MountedOn;
		return ClipPlayback.Instant;
	}
}
