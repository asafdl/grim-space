using System;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Presentation.Replay.Clips;

public sealed class FlakActionClip : IReplayClip
{
	public Type ActionType => typeof(FlakAction);

	public ClipPlayback Play(IAction action, ReplayClipContext context)
	{
		var flak = (FlakAction)action;
		var effect = ScrapDroneFlakEffect.Play(
			context.HazardBursts,
			flak,
			context.ReplayState,
			context.UnitViews);

		return ClipPlayback.Pause(effect.ImpactSeconds);
	}
}
