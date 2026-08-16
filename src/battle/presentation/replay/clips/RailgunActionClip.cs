using Godot;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Replay.Clips;

public sealed class RailgunActionClip : IReplayClip
{
	private static readonly Color Tint = new(0.85f, 0.35f, 1f, 0.55f);

	private static readonly float ReachCells =
		CombatConfig.RailgunLineLength
		+ CombatConfig.RailgunPyramidRange
		+ 0.7f;

	public Type ActionType => typeof(RailgunAction);

	public ClipPlayback Play(IAction action, ReplayClipContext context)
	{
		var railgun = (RailgunAction)action;
		var state = context.ReplayState.StateOf(railgun.ActorId);

		context.HazardBursts.PlayShotBurst(
			state.Position,
			ToVector3(state.Fore),
			ReachCells,
			Tint,
			ReplayTiming.WeaponBurstSeconds);

		return ClipPlayback.Pause(ReplayTiming.WeaponBurstSeconds);
	}

	private static Vector3 ToVector3(Coord coord) =>
		new(coord.X, coord.Y, coord.Z);
}
