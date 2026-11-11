using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Core.Actions;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Presentation.Replay.Clips;

public sealed class HighVelocityRodActionClip : IReplayClip
{
	public Type ActionType => typeof(HighVelocityRodAction);

	public ClipPlayback Play(IAction action, ReplayClipContext context)
	{
		var rod = (HighVelocityRodAction)action;
		var state = context.ReplayState.StateOf(rod.ActorId);
		var mount = new AbilityMount(EAbilityKind.HighVelocityRod, rod.MountedOn);
		state.MountRuntimeFor(mount).UsesRemaining--;
		var area = context.FollowingAreaDamage(rod.ActorId, EImpactCause.HighVelocityRod)
			?? throw new InvalidOperationException("High Velocity Rod replay is missing resolved area facts.");
		if (area.Cells.Count == 0)
			return ClipPlayback.Instant;
		var end = area.Cells.MaxBy(cell => state.Position.ManhattanDistanceTo(cell));
		context.HazardBursts.PlayRodShot(state.Position, end, state.Fore);
		return ClipPlayback.Pause(HighVelocityRodEffect.ImpactSeconds);
	}
}
