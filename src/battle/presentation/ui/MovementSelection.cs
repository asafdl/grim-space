using Godot;
using GrimSpace.Battle.Player;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Ui;

public static class MovementSelection
{
	private const float PickRadiusPixels = 22f;
	private const float HeadingHandleWorldOffset = 1.25f;

	public readonly record struct HeadingHandle(Coord Heading, Vector2 Position, float Rotation);

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

	public static Coord? PickHeading(
		Camera3D camera,
		Vector2 screenPos,
		Coord destination,
		IReadOnlyList<Coord> reachableHeadings)
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
		foreach (var handle in ProjectHeadingHandles(camera, destination, reachableHeadings))
		{
			var axis = handle.Position - center;
			var dot = drag.Normalized().Dot(axis.Normalized());
			if (dot <= bestDot)
				continue;
			bestDot = dot;
			best = handle.Heading;
		}

		return best;
	}

	public static IReadOnlyList<HeadingHandle> ProjectHeadingHandles(
		Camera3D camera,
		Coord destination,
		IReadOnlyCollection<Coord> headings)
	{
		var centerWorld = WorldMapping.ToWorld(destination);
		if (camera.IsPositionBehind(centerWorld))
			return [];

		var center = camera.UnprojectPosition(centerWorld);
		var handles = new List<HeadingHandle>(headings.Count);
		foreach (var heading in headings)
		{
			var worldDirection = new Vector3(heading.X, heading.Y, heading.Z);
			var projected = camera.UnprojectPosition(
				centerWorld + worldDirection * HeadingHandleWorldOffset) - center;
			if (projected.LengthSquared() < 16f)
			{
				var depth = worldDirection.Dot(camera.GlobalBasis.Z);
				projected = Vector2.Up * (depth >= 0f ? 1f : -1f);
			}

			var direction = projected.Normalized();
			handles.Add(new HeadingHandle(
				heading,
				center + direction * HeadingHandleDistance(camera, centerWorld),
				direction.Angle()));
		}

		return handles;
	}

	private static float HeadingHandleDistance(Camera3D camera, Vector3 centerWorld)
	{
		var edge = camera.UnprojectPosition(centerWorld + Vector3.Right * WorldMapping.CellSize * 0.65f);
		var center = camera.UnprojectPosition(centerWorld);
		return System.Math.Clamp(edge.DistanceTo(center), 42f, 72f);
	}
}
