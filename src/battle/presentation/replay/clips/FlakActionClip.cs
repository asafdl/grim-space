using Godot;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Replay.Clips;

public sealed class FlakActionClip : IReplayClip
{
	private static readonly Color PortTint = new(0.95f, 0.55f, 0.18f, 0.50f);
	private static readonly Color StarboardTint = new(0.98f, 0.78f, 0.22f, 0.50f);

	private static readonly float ReachCells = CombatConfig.FlakRange + 1.6f;

	public Type ActionType => typeof(FlakAction);

	public ClipPlayback Play(IAction action, ReplayClipContext context)
	{
		var flak = (FlakAction)action;
		var state = context.ReplayState.StateOf(flak.ActorId);
		var starboard = ToVector3(state.Starboard);
		var direction = flak.MountedOn == ESpatialOrientation.Port ? -starboard : starboard;
		var tint = flak.MountedOn == ESpatialOrientation.Port ? PortTint : StarboardTint;

		context.HazardBursts.PlayShotBurst(
			state.Position,
			direction,
			ReachCells,
			tint,
			ReplayTiming.WeaponBurstSeconds);

		return ClipPlayback.Pause(ReplayTiming.WeaponBurstSeconds);
	}

	private static Vector3 ToVector3(Coord coord) =>
		new(coord.X, coord.Y, coord.Z);
}
