using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Presentation.Replay.Clips;

public sealed class OverchargeEnginesActionClip : IReplayClip
{
	public Type ActionType => typeof(OverchargeEnginesAction);

	public ClipPlayback Play(IAction action, ReplayClipContext context)
	{
		if (!context.UnitViews.TryGetValue(action.ActorId, out var view))
			return ClipPlayback.Instant;

		view.AnimateEngineOvercharge();
		return ClipPlayback.Pause(UnitView.EngineOverchargeSeconds);
	}
}
