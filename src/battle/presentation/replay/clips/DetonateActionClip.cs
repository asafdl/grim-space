using System;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Presentation.Replay.Clips;

public sealed class DetonateActionClip : IReplayClip
{
	public Type ActionType => typeof(DetonateAction);

	public ClipPlayback Play(IAction action, ReplayClipContext context)
	{
		var detonate = (DetonateAction)action;
		var actor = context.ReplayState.StateOf(detonate.ActorId);
		var blastRadius = actor.RequireProjectile().BlastRadius;

		context.DismissUnitPresentation(detonate.ActorId);

		var duration = context.HazardBursts.PlayVoidBomb(
			actor.Position,
			blastRadius);

		return ClipPlayback.Pause(duration);
	}
}
