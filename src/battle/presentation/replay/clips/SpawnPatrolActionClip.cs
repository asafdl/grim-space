using GrimSpace.Battle.Actions;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Presentation.Replay.Clips;

public sealed class SpawnPatrolActionClip : IReplayClip
{
	public Type ActionType => typeof(SpawnPatrolAction);

	public ClipPlayback Play(IAction action, ReplayClipContext context) => ClipPlayback.Instant;
}
