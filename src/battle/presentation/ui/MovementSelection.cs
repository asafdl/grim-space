using Godot;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Ui;

public static class MovementSelection
{
	private const float PickRadiusPixels = 22f;

	public static int? PickPathIndex(Camera3D camera, Vector2 screenPos, IReadOnlyList<MovePathOption> paths)
	{
		if (paths.Count == 0)
			return null;

		int? bestIndex = null;
		var bestDistance = PickRadiusPixels;
		var seen = new HashSet<Coord>();

		for (var i = 0; i < paths.Count; i++)
		{
			if (!seen.Add(paths[i].EndPosition))
				continue;
			var world = WorldMapping.ToWorld(paths[i].EndPosition);
			if (camera.IsPositionBehind(world))
				continue;
			var distance = camera.UnprojectPosition(world).DistanceTo(screenPos);
			if (distance >= bestDistance)
				continue;

			bestDistance = distance;
			bestIndex = i;
		}

		return bestIndex;
	}

	public static Coord? PickHeading(Camera3D camera, Vector2 screenPos, Coord destination)
	{
		var centerWorld = WorldMapping.ToWorld(destination);
		if (camera.IsPositionBehind(centerWorld))
			return null;

		var center = camera.UnprojectPosition(centerWorld);
		var drag = screenPos - center;
		if (drag.LengthSquared() < 16f)
			return null;

		Coord? best = null;
		var bestDot = float.NegativeInfinity;
		foreach (var heading in MovePose.Headings)
		{
			var handle = camera.UnprojectPosition(centerWorld + new Vector3(heading.X, heading.Y, heading.Z));
			var axis = handle - center;
			if (axis.LengthSquared() < 0.01f)
				continue;

			var dot = drag.Normalized().Dot(axis.Normalized());
			if (dot <= bestDot)
				continue;
			bestDot = dot;
			best = heading;
		}

		return best;
	}
}
