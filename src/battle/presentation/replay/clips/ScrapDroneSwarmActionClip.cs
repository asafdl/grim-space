using System;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Presentation.Replay.Clips;

public sealed class ScrapDroneSwarmActionClip : IReplayClip
{
	public Type ActionType => typeof(ScrapDroneSwarmAction);

	public ClipPlayback Play(IAction action, ReplayClipContext context)
	{
		var swarm = (ScrapDroneSwarmAction)action;
		var effect = ScrapDroneAttackEffect.Play(
			context.HazardBursts,
			swarm,
			context.ReplayState,
			context.UnitViews);

		return ClipPlayback.Pause(effect.ImpactSeconds);
	}
}
