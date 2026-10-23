using System;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Camera;
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

		var deaths = VoidBombDetonationShake.CountDeaths(
			detonate.ActorId,
			context.FollowingRecords,
			context.EndStates);
		if (deaths > 0)
		{
			context.ReportImpactShake?.Invoke(ImpactShakeScale.ForVoidBombDetonation(
				deaths,
				WorldMapping.ToWorld(actor.Position)));
		}

		return ClipPlayback.Pause(duration);
	}
}
