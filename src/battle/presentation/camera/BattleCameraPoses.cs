using Godot;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Camera;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Camera;

internal static class BattleCameraPoses
{
	private const float PlayerDistance = 18f;
	private const float OverviewUsableScreen = 0.8f;
	private const float OverviewShipPadding = 1f;
	private const float OverviewPreferredDistance = 22f;
	private const float OverviewComfortDistance = 36f;

	public static OrbitPose PlayerRearQuarter(State state)
	{
		var direction = ToVector(Coord.Cross(state.Dorsal, state.Fore) - state.Fore);
		var (yaw, pitch) = Controller.OrbitAnglesForDirection(direction);
		return new OrbitPose
		{
			Pivot = WorldMapping.ToWorld(state.Position),
			Distance = PlayerDistance,
			Yaw = yaw,
			Pitch = pitch,
		};
	}

	public static OrbitPose Overview(
		IReadOnlyList<Vector3> points,
		OrbitPose source,
		float verticalFovDegrees,
		float aspect,
		in OrbitLimits limits,
		IReadOnlyList<Vector3>? priorityPoints = null,
		Func<Vector3, IReadOnlyList<Vector3>, int>? countOccluded = null)
	{
		ArgumentNullException.ThrowIfNull(points);
		if (points.Count == 0)
			throw new ArgumentException("Overview requires at least one ship position.", nameof(points));
		if (!float.IsFinite(verticalFovDegrees) || verticalFovDegrees <= 0f || verticalFovDegrees >= 180f)
			throw new ArgumentOutOfRangeException(nameof(verticalFovDegrees));
		if (!float.IsFinite(aspect) || aspect <= 0f)
			throw new ArgumentOutOfRangeException(nameof(aspect));
		if (limits.MinDistance <= 0f || limits.MaxDistance < limits.MinDistance
			|| limits.MinPitch > limits.MaxPitch)
			throw new ArgumentException("Invalid camera limits.", nameof(limits));

		priorityPoints ??= points;
		if (priorityPoints.Count == 0 || priorityPoints.Any(point => !points.Contains(point)))
			throw new ArgumentException("Priority ships must be part of the overview.", nameof(priorityPoints));

		foreach (var point in points)
		{
			if (!float.IsFinite(point.X) || !float.IsFinite(point.Y) || !float.IsFinite(point.Z))
				throw new ArgumentException("Ship positions must be finite.", nameof(points));
		}

		var preferredDirection = source.Offset().Normalized();
		var pitch = MathF.Asin(1f / MathF.Sqrt(3f));
		var best = default(OrbitPose);
		var bestIncluded = -1;
		var bestPriorityDistance = float.PositiveInfinity;
		var bestPriorityOccluded = int.MaxValue;
		var bestOccluded = int.MaxValue;
		var bestSideMismatch = true;
		var bestQuality = float.NegativeInfinity;
		var bestAlignment = float.NegativeInfinity;
		var verticalTan = MathF.Tan(Mathf.DegToRad(verticalFovDegrees) * 0.5f) * OverviewUsableScreen;
		var horizontalTan = verticalTan * aspect;
		var minDistance = Mathf.Clamp(MathF.Min(source.Distance, OverviewPreferredDistance), limits.MinDistance, limits.MaxDistance);
		var additionalPoints = points.Where(point => !priorityPoints.Contains(point))
			.OrderBy(point => priorityPoints.Min(priority => point.DistanceSquaredTo(priority)))
			.ToArray();

		foreach (var pitchSign in new[] { 1f, -1f })
		for (var directionIndex = 0; directionIndex < 8; directionIndex++)
		{
			var candidate = new OrbitPose
			{
				Yaw = Mathf.DegToRad(directionIndex * 45f),
				Pitch = Mathf.Clamp(pitch * pitchSign, limits.MinPitch, limits.MaxPitch),
				Distance = 1f,
			};
			var direction = candidate.Offset();
			var right = Vector3.Up.Cross(direction).Normalized();
			var up = direction.Cross(right).Normalized();
			var included = priorityPoints.Distinct().ToList();
			var (center, requiredDistance) = Fit(included);
			var priorityDistance = requiredDistance;
			var comfortLimit = MathF.Max(OverviewComfortDistance, priorityDistance);
			foreach (var point in additionalPoints)
			{
				included.Add(point);
				var expanded = Fit(included);
				if (expanded.Distance > comfortLimit)
				{
					included.RemoveAt(included.Count - 1);
					continue;
				}
				(center, requiredDistance) = expanded;
			}

			var separation = 1f;
			for (var i = 0; i < included.Count; i++)
			for (var j = i + 1; j < included.Count; j++)
			{
				var delta = included[j] - included[i];
				var lengthSquared = delta.LengthSquared();
				if (lengthSquared > 0.0001f)
				{
					var depth = delta.Dot(direction);
					separation = MathF.Min(separation, 1f - depth * depth / lengthSquared);
				}
			}

			var alignment = direction.Dot(preferredDirection);
			var quality = separation * 0.65f
				- Mathf.Clamp((requiredDistance - minDistance) / OverviewComfortDistance, 0f, 1f) * 0.35f
				+ alignment * 0.15f;
			var sideMismatch = MathF.Abs(source.Pitch) >= 0.15f && source.Pitch * candidate.Pitch < 0f;
			var priorityOverflow = MathF.Max(0f, priorityDistance - limits.MaxDistance);
			candidate.Pivot = center;
			candidate.Distance = Mathf.Clamp(requiredDistance, limits.MinDistance, limits.MaxDistance);
			var cameraPosition = candidate.CameraPosition();
			var priorityOccluded = countOccluded?.Invoke(cameraPosition, priorityPoints) ?? 0;
			var occluded = countOccluded?.Invoke(cameraPosition, included) ?? 0;
			if (priorityOverflow > bestPriorityDistance + 0.0001f
				|| (MathF.Abs(priorityOverflow - bestPriorityDistance) <= 0.0001f
					&& (priorityOccluded > bestPriorityOccluded
						|| (priorityOccluded == bestPriorityOccluded
							&& (included.Count < bestIncluded
								|| (included.Count == bestIncluded && occluded > bestOccluded)
								|| (included.Count == bestIncluded && occluded == bestOccluded
									&& sideMismatch && !bestSideMismatch)
								|| (included.Count == bestIncluded && occluded == bestOccluded
									&& sideMismatch == bestSideMismatch
									&& quality < bestQuality - 0.0001f)
								|| (included.Count == bestIncluded && occluded == bestOccluded
									&& sideMismatch == bestSideMismatch
									&& MathF.Abs(quality - bestQuality) <= 0.0001f
									&& alignment <= bestAlignment))))))
				continue;

			best = candidate;
			bestPriorityDistance = priorityOverflow;
			bestPriorityOccluded = priorityOccluded;
			bestIncluded = included.Count;
			bestOccluded = occluded;
			bestSideMismatch = sideMismatch;
			bestQuality = quality;
			bestAlignment = alignment;

			(Vector3 Center, float Distance) Fit(IReadOnlyList<Vector3> ships)
			{
				var min = ships[0];
				var max = ships[0];
				foreach (var point in ships)
				{
					min = new Vector3(MathF.Min(min.X, point.X), MathF.Min(min.Y, point.Y), MathF.Min(min.Z, point.Z));
					max = new Vector3(MathF.Max(max.X, point.X), MathF.Max(max.Y, point.Y), MathF.Max(max.Z, point.Z));
				}
				var midpoint = (min + max) * 0.5f;
				var distance = minDistance;
				foreach (var point in ships)
				{
					var relative = point - midpoint;
					distance = MathF.Max(distance, relative.Dot(direction) + MathF.Max(
						(MathF.Abs(relative.Dot(right)) + OverviewShipPadding) / horizontalTan,
						(MathF.Abs(relative.Dot(up)) + OverviewShipPadding) / verticalTan));
				}
				return (midpoint, distance);
			}
		}

		return best;
	}

	private static Vector3 ToVector(Coord coord) =>
		new(coord.X, coord.Y, coord.Z);
}
