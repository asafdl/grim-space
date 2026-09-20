using Godot;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Presentation.Replay.Clips;

public sealed class RailgunActionClip : IReplayClip
{
	private static readonly Color Tint = new(0.85f, 0.35f, 1f, 0.55f);

	public Type ActionType => typeof(RailgunAction);

	public ClipPlayback Play(IAction action, ReplayClipContext context)
	{
		var railgun = (RailgunAction)action;
		var state = context.ReplayState.StateOf(railgun.ActorId);
		var reachCells = state.FindInstalled(EAbilityKind.Railgun, railgun.MountedOn)?.Spec is RailgunSpec railgunSpec
			? AbilityReach.RailgunReplayShotLength(railgunSpec)
			: 10.7f;

		context.HazardBursts.PlayShotBurst(
			state.Position,
			ToVector3(state.Fore),
			reachCells,
			Tint,
			ReplayTiming.WeaponBurstSeconds);

		return ClipPlayback.Pause(ReplayTiming.WeaponBurstSeconds);
	}

	private static Vector3 ToVector3(Coord coord) =>
		new(coord.X, coord.Y, coord.Z);
}
