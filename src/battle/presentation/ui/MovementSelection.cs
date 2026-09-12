using Godot;
using GrimSpace.Battle.Player;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Ui;

public static class MovementSelection
{
	private const float PickRadiusPixels = 22f;
	private const float DepthDirectionThreshold = 0.88f;
	private const float DepthHandleOffsetPixels = 52f;
	private const float MinimumProjectedDepthOffsetPixels = 34f;

	public enum HeadingHandleKind { Arrow, TowardCamera, AwayFromCamera }

	public readonly record struct HeadingHandle(
		Coord Heading,
		Vector2 Position,
		float Rotation,
		HeadingHandleKind Kind);

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
				centerWorld + worldDirection * WorldMapping.CellSize) - center;
			var kind = HeadingHandleKindFor(worldDirection, camera.GlobalBasis.Z);
			if (kind != HeadingHandleKind.Arrow
				&& projected.LengthSquared() < MinimumProjectedDepthOffsetPixels * MinimumProjectedDepthOffsetPixels)
			{
				projected = Vector2.Up
					* DepthHandleOffsetPixels
					* (kind == HeadingHandleKind.TowardCamera ? -1f : 1f);
			}

			var direction = projected.Normalized();
			handles.Add(new HeadingHandle(
				heading,
				center + projected,
				direction.Angle(),
				kind));
		}

		return handles;
	}

	internal static HeadingHandleKind HeadingHandleKindFor(Vector3 direction, Vector3 cameraBack)
	{
		var depth = direction.Normalized().Dot(cameraBack.Normalized());
		if (Mathf.Abs(depth) < DepthDirectionThreshold)
			return HeadingHandleKind.Arrow;

		return depth > 0f
			? HeadingHandleKind.TowardCamera
			: HeadingHandleKind.AwayFromCamera;
	}
}
