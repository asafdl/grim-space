using Godot;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Presentation.Replay.Clips;

public sealed class LightningCannonActionClip : IReplayClip
{
	public Type ActionType => typeof(LightningCannonAction);

	public ClipPlayback Play(IAction action, ReplayClipContext context)
	{
		var lightningCannon = (LightningCannonAction)action;
		var state = context.ReplayState.StateOf(lightningCannon.ActorId);
		var spec = state.FindInstalled(EAbilityKind.LightningCannon, lightningCannon.MountedOn)?.Spec as LightningCannonSpec
			?? throw new InvalidOperationException($"Lightning cannon replay actor '{lightningCannon.ActorId}' has no lightning cannon installed on {lightningCannon.MountedOn}.");

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
