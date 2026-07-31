using GrimSpace.Battle.Actions;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Presentation.Replay.Clips;

public sealed class ResolveHazardClip : IReplayClip
{
	public Type ActionType => typeof(ResolveHazardAction);

	public ClipPlayback Play(IAction action, ReplayClipContext context)
	{
		var resolve = (ResolveHazardAction)action;
		foreach (var unitId in context.ReplayState.ApplyResolveHazard(resolve))
			context.SyncUnit(unitId);

		return ClipPlayback.Instant;
	}
}
