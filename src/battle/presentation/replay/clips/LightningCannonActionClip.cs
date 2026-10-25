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
		var path = BuildVisualPath(state.Position, direction, spec, areaFacts);
		var dorsal = ToVector3(frame.Dorsal);
		var cellSize = WorldMapping.CellSize;
		var segmentCount = path.Count - 1;
		for (var segment = 0; segment < segmentCount; segment++)
		{
			var chainHop = segment > 0;
			LightningCannonEffect.FireAlongPath(
				context.HazardBursts,
				[path[segment], path[segment + 1]],
				dorsal,
				cellSize,
				strikeSeconds: chainHop
					? LightningCannonEffect.ChainHopStrikeSeconds
					: LightningCannonEffect.StrikeSeconds,
				startDelay: LightningCannonEffect.ChainSegmentStartDelay(segment));
		}

		return ClipPlayback.Pause(LightningCannonEffect.ChainSequencePauseSeconds(segmentCount));
	}

	private static Vector3 ToVector3(Coord coord) =>
		new(coord.X, coord.Y, coord.Z);

	private static IReadOnlyList<Vector3> BuildVisualPath(
		Coord origin,
		Coord direction,
		LightningCannonSpec spec,
		AreaDamageFacts areaFacts)
	{
		var path = new List<Vector3> { WorldMapping.ToWorld(origin) };
		if (areaFacts.ChainHops is { Count: > 0 } chainHops)
		{
			foreach (var hop in chainHops)
				path.Add(WorldMapping.ToWorld(hop));
			return path;
		}

		var beamEnd = origin + direction * BeamVisibleLength(origin, direction, spec, areaFacts.Cells);
		path.Add(WorldMapping.ToWorld(beamEnd));
		return path;
	}

	private static int BeamVisibleLength(
		Coord origin,
		Coord direction,
		LightningCannonSpec spec,
		IReadOnlySet<Coord> beamCells)
	{
		if (beamCells.Count == 0)
			return spec.LineLength;

		var maxDistance = 0;
		foreach (var cell in beamCells)
		{
			var distance = RayDistance(origin, direction, cell);
			if (distance > maxDistance)
				maxDistance = distance;
		}

		return maxDistance;
	}

	private static int RayDistance(Coord origin, Coord direction, Coord cell)
	{
		if (direction.X != 0)
			return (cell.X - origin.X) / direction.X;
		if (direction.Y != 0)
			return (cell.Y - origin.Y) / direction.Y;
		if (direction.Z != 0)
			return (cell.Z - origin.Z) / direction.Z;
		return 0;
	}
}
