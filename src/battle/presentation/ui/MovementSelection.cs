using Godot;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Picking;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Ui;

public static class MovementSelection
{
	private const float MinRadiusPx = 24f;
	private const float MaxRadiusPx = 64f;
	private const float RadiusMultiplier = 0.65f;
	private const float DepthDirectionThreshold = 0.88f;
	private const float DepthHandleOffsetPixels = 52f;
	private const float MinimumProjectedDepthOffsetPixels = 34f;

	public enum HeadingHandleKind { Arrow, TowardCamera, AwayFromCamera }

	public readonly record struct HeadingHandle(
		Coord Heading,
		Vector2 Position,
		float Rotation,
		HeadingHandleKind Kind);

	public static Coord? PickCoordinate(
		Camera3D camera,
		Viewport viewport,
		Vector2 screenPos,
		IReadOnlyList<MovePathOption> paths,
		Coord? currentHovered = null)
	{
		var candidates = BuildCandidates(camera, viewport, screenPos, paths);
		return MovementPick.Resolve(candidates, currentHovered);
	}

	public static MovePathOption? ResolveOption(
		IReadOnlyList<MovePathOption> paths,
		Coord? coordinate)
	{
		if (coordinate is not Coord cell)
			return null;

		foreach (var option in paths)
		{
			if (option.EndPosition == cell && option.Steps.Count > 0)
				return option;
		}

		return null;
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

	private static List<MovementPickCandidate> BuildCandidates(
		Camera3D camera,
		Viewport viewport,
		Vector2 screenPos,
		IReadOnlyList<MovePathOption> paths)
	{
		var viewportRect = viewport.GetVisibleRect();
		if (!viewportRect.HasPoint(screenPos))
			return [];

		var candidates = new List<MovementPickCandidate>();
		var seen = new HashSet<Coord>();
		var near = camera.Near;

		foreach (var option in paths)
		{
			if (option.Steps.Count == 0 || !seen.Add(option.EndPosition))
				continue;

			var centerWorld = WorldMapping.ToWorld(option.EndPosition);
			if (camera.IsPositionBehind(centerWorld))
				continue;

			var depth = CameraForwardDepth(camera, centerWorld);
			if (depth <= near)
				continue;

			var projectedCenter = camera.UnprojectPosition(centerWorld);
			if (!IsFinite(projectedCenter) || !viewportRect.HasPoint(projectedCenter))
				continue;

			var radiusPx = ComputeRadiusPx(camera, centerWorld, depth, near);
			if (!float.IsFinite(radiusPx))
				continue;

			var distancePx = projectedCenter.DistanceTo(screenPos);
			candidates.Add(new MovementPickCandidate(
				option.EndPosition,
				distancePx,
				radiusPx,
				depth));
		}

		return candidates;
	}

	private static float ComputeRadiusPx(
		Camera3D camera,
		Vector3 centerWorld,
		float centerDepth,
		float near)
	{
		var half = WorldMapping.CellSize * 0.5f;
		var projected = new List<Vector2> { camera.UnprojectPosition(centerWorld) };
		var anyCornerInvalid = false;

		for (var x = -1; x <= 1; x += 2)
		{
			for (var y = -1; y <= 1; y += 2)
			{
				for (var z = -1; z <= 1; z += 2)
				{
					var corner = centerWorld + new Vector3(x, y, z) * half;
					if (camera.IsPositionBehind(corner))
					{
						anyCornerInvalid = true;
						continue;
					}

					var cornerDepth = CameraForwardDepth(camera, corner);
					if (cornerDepth <= near)
					{
						anyCornerInvalid = true;
						continue;
					}

					var projectedCorner = camera.UnprojectPosition(corner);
					if (!IsFinite(projectedCorner))
					{
						anyCornerInvalid = true;
						continue;
					}

					projected.Add(projectedCorner);
				}
			}
		}

		if (anyCornerInvalid)
			return MaxRadiusPx;

		var minX = projected[0].X;
		var maxX = projected[0].X;
		var minY = projected[0].Y;
		var maxY = projected[0].Y;
		for (var i = 1; i < projected.Count; i++)
		{
			minX = System.Math.Min(minX, projected[i].X);
			maxX = System.Math.Max(maxX, projected[i].X);
			minY = System.Math.Min(minY, projected[i].Y);
			maxY = System.Math.Max(maxY, projected[i].Y);
		}

		var widthPx = maxX - minX;
		var heightPx = maxY - minY;
		var extentPx = 0.5f * Mathf.Sqrt(widthPx * widthPx + heightPx * heightPx);
		return Mathf.Clamp(RadiusMultiplier * extentPx, MinRadiusPx, MaxRadiusPx);
	}

	private static float CameraForwardDepth(Camera3D camera, Vector3 world) =>
		-(camera.GlobalTransform.AffineInverse() * world).Z;

	private static bool IsFinite(Vector2 value) =>
		float.IsFinite(value.X) && float.IsFinite(value.Y);
}
