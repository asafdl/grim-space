using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Core.Actions;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Presentation.Replay.Clips;

public sealed class RoadsWeDontNeedRoadsActionClip : IReplayClip
{
	public Type ActionType => typeof(RoadsWeDontNeedRoadsAction);

	public ClipPlayback Play(IAction action, ReplayClipContext context)
	{
		var jump = (RoadsWeDontNeedRoadsAction)action;
		var moved = context.FollowingMoved(jump.ActorId)
			?? throw new InvalidOperationException("Jump replay is missing resolved movement facts.");
		var state = context.ReplayState.StateOf(jump.ActorId);
		var installed = state.FindInstalled(EAbilityKind.RoadsWeDontNeedRoads)
			?? throw new InvalidOperationException("Jump replay actor has no jump installed.");
		if (installed.Spec is not RoadsWeDontNeedRoadsSpec spec)
			throw new InvalidOperationException("Jump replay spec missing.");
		state.Position = moved.To;
		state.MountRuntimeFor(installed.Mount).CooldownRemaining = spec.CooldownTurns;
		context.UnitViews[jump.ActorId].AnimateTeleport(state);
		return ClipPlayback.Pause(PixelTeleportEffect.DurationSeconds);
	}
}
