using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Core.Actions;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Presentation.Replay.Clips;

public sealed class HighSpeedRodActionClip : IReplayClip
{
	public Type ActionType => typeof(HighSpeedRodAction);

	public ClipPlayback Play(IAction action, ReplayClipContext context)
	{
		var rod = (HighSpeedRodAction)action;
		var state = context.ReplayState.StateOf(rod.ActorId);
		var mount = new AbilityMount(EAbilityKind.HighSpeedRod, rod.MountedOn);
		state.MountRuntimeFor(mount).UsesRemaining--;
		var area = context.FollowingAreaDamage(rod.ActorId, EImpactCause.HighSpeedRod)
			?? throw new InvalidOperationException("High-Speed Rod replay is missing resolved area facts.");
		if (area.Cells.Count == 0)
			return ClipPlayback.Instant;
		var end = area.Cells.MaxBy(cell => state.Position.ManhattanDistanceTo(cell));
		const double duration = 0.18;
		context.HazardBursts.PlayRodShot(state.Position, end, duration);
		return ClipPlayback.Pause(duration);
	}
}
