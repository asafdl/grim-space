using GrimSpace.Battle.Actions;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Presentation.Replay.Clips;

public sealed class HeatVentsActionClip : IReplayClip
{
	public Type ActionType => typeof(HeatVentsAction);

	public ClipPlayback Play(IAction action, ReplayClipContext context)
	{
		if (context.UnitViews.TryGetValue(action.ActorId, out var view))
			view.PlayHeatVentFumes();
		return ClipPlayback.Instant;
	}
}
