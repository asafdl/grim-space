using GrimSpace.Math.Grid;

namespace GrimSpace.Math.Routes;

public static class PiecewiseRouteSampler
{
	public static double TimeRequired(IReadOnlyList<RouteSegment> segments, double baseSpeed) =>
		segments.Sum(segment => segment.Length / (baseSpeed * segment.SpeedMultiplier));

	public static double DistanceTraveledAtElapsed(
		IReadOnlyList<RouteSegment> segments,
		double elapsedTime,
		double baseSpeed)
	{
		if (segments.Count == 0 || elapsedTime <= 0 || baseSpeed <= 0)
			return 0;

		return Walk(segments, elapsedTime, baseSpeed).DistanceTraveled;
	}

	public static PiecewiseRouteSample SampleContinuousAtElapsed(
		IReadOnlyList<RouteSegment> segments,
		double elapsedTime,
		double baseSpeed)
	{
		var walk = Walk(segments, elapsedTime, baseSpeed);
		var segment = segments[walk.SegmentIndex];
		return new PiecewiseRouteSample(
			PolylineSampler.SampleContinuous(segment.Points, walk.ArcIntoSegment),
			walk.SegmentIndex);
	}

	public static (Coord Position, Coord Tangent) SampleAtElapsed(
		IReadOnlyList<RouteSegment> segments,
		double elapsedTime,
		double baseSpeed)
	{
		var sample = SampleContinuousAtElapsed(segments, elapsedTime, baseSpeed);
		return sample.Route.ToRoundedCoord();
	}

	private static WalkResult Walk(
		IReadOnlyList<RouteSegment> segments,
		double elapsedTime,
		double baseSpeed)
	{
		var remaining = System.Math.Max(0, elapsedTime);
		var traveled = 0.0;
		for (var segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
		{
			var segment = segments[segmentIndex];
			var speed = baseSpeed * segment.SpeedMultiplier;
			if (speed <= 0)
				continue;

			var timeForSegment = segment.Length / speed;
			if (remaining < timeForSegment)
			{
				return new WalkResult(
					segmentIndex,
					remaining * speed,
					traveled + remaining * speed);
			}

			traveled += segment.Length;
			remaining -= timeForSegment;
		}

		var lastIndex = segments.Count - 1;
		var last = segments[lastIndex];
		return new WalkResult(lastIndex, last.Length, traveled);
	}

	private readonly record struct WalkResult(
		int SegmentIndex,
		double ArcIntoSegment,
		double DistanceTraveled);
}
