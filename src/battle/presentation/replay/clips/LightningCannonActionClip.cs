using Godot;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Battle.Spatial;
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
		var areaFacts = context.FollowingAreaDamage(
			lightningCannon.ActorId,
			EImpactCause.LightningCannonBurst)
			?? throw new InvalidOperationException(
				$"Lightning cannon replay actor '{lightningCannon.ActorId}' has no resolved area facts.");
		var frame = BodyFrame.From(state);
		var direction = frame.Step(lightningCannon.MountedOn);
		var reach = ResolveVisualReach(state.Position, direction, frame, spec, areaFacts.Cells);

		LightningCannonEffect.Fire(
			context.HazardBursts,
			WorldMapping.ToWorld(state.Position),
			ToVector3(direction),
			ToVector3(frame.Dorsal),
			WorldMapping.CellSize,
			reach.LineLength,
			reach.PyramidRange);

		return ClipPlayback.Pause(LightningCannonEffect.StrikeSeconds);
	}

	private static Vector3 ToVector3(Coord coord) =>
		new(coord.X, coord.Y, coord.Z);

	private static VisualReach ResolveVisualReach(
		Coord origin,
		Coord direction,
		BodyFrame frame,
		LightningCannonSpec spec,
		IReadOnlySet<Coord> affectedCells)
	{
		for (var distance = 1; distance <= spec.LineLength; distance++)
		{
			if (!affectedCells.Contains(origin + direction * distance))
				return new VisualReach(distance - 0.5f, 0);
		}

		var fullAreaVisible = spec
			.GetArea(origin, direction, frame.Fore, frame.Dorsal)
			.All(affectedCells.Contains);
		return new VisualReach(spec.LineLength, fullAreaVisible ? spec.PyramidRange : 0);
	}

	private readonly record struct VisualReach(float LineLength, int PyramidRange);
}
