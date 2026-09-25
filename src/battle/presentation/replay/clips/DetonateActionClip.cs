using Godot;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Presentation.Replay.Clips;

public sealed class DetonateActionClip : IReplayClip
{
	private static readonly Color Tint = new(0.2f, 0.85f, 0.55f, 0.50f);

	public Type ActionType => typeof(DetonateAction);

	public ClipPlayback Play(IAction action, ReplayClipContext context)
	{
		var detonate = (DetonateAction)action;
		var actor = context.ReplayState.StateOf(detonate.ActorId);
		var blastRadius = actor.RequireProjectile().BlastRadius;

		context.HazardBursts.PlayRadialBurst(
			actor.Position,
			blastRadius,
			Tint,
			ReplayTiming.WeaponBurstSeconds);

		return ClipPlayback.Pause(ReplayTiming.WeaponBurstSeconds);
	}
}
