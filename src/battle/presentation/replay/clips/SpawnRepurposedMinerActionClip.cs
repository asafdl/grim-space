using GrimSpace.Battle.Actions;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Presentation.Replay.Clips;

public sealed class SpawnRepurposedMinerActionClip : IReplayClip
{
	public Type ActionType => typeof(SpawnRepurposedMinerAction);

	public ClipPlayback Play(IAction action, ReplayClipContext context) => ClipPlayback.Instant;
}
