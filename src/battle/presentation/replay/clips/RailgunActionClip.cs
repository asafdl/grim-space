using Godot;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Presentation.Replay.Clips;

public sealed class RailgunActionClip : IReplayClip
{
	public Type ActionType => typeof(RailgunAction);

	public ClipPlayback Play(IAction action, ReplayClipContext context)
	{
		var railgun = (RailgunAction)action;
		var state = context.ReplayState.StateOf(railgun.ActorId);
		var spec = state.FindInstalled(EAbilityKind.Railgun, railgun.MountedOn)?.Spec as RailgunSpec
			?? throw new InvalidOperationException($"Railgun replay actor '{railgun.ActorId}' has no railgun installed on {railgun.MountedOn}.");

		LightningCannonEffect.Fire(
			context.HazardBursts,
			WorldMapping.ToWorld(state.Position),
			ToVector3(state.Fore),
			ToVector3(state.Dorsal),
			WorldMapping.CellSize,
			spec.LineLength,
			spec.PyramidRange);

		return ClipPlayback.Pause(LightningCannonEffect.StrikeSeconds);
	}

	private static Vector3 ToVector3(Coord coord) =>
		new(coord.X, coord.Y, coord.Z);
}
