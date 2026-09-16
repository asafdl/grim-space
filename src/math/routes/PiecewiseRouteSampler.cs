using GrimSpace.Math.Grid;

namespace GrimSpace.Math.Routes;

public static class PiecewiseRouteSampler
{
	public static double TimeRequired(IReadOnlyList<RouteSegment> segments, double baseSpeed) =>
		segments.Sum(segment => segment.Length / (baseSpeed * segment.SpeedMultiplier));

	public static PiecewiseRouteSample SampleContinuousAtElapsed(
		IReadOnlyList<RouteSegment> segments,
		double elapsedTime,
		double baseSpeed)
	{
		var remaining = elapsedTime;
		for (var segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
		{
			var segment = segments[segmentIndex];
			var timeForSegment = segment.Length / (baseSpeed * segment.SpeedMultiplier);
			if (remaining < timeForSegment)
			{
				var progress = remaining * baseSpeed * segment.SpeedMultiplier;
				return new PiecewiseRouteSample(
					PolylineSampler.SampleContinuous(segment.Points, progress),
					segmentIndex);
			}

			remaining -= timeForSegment;
		}

		var lastIndex = segments.Count - 1;
		var last = segments[lastIndex];
		return new PiecewiseRouteSample(
			PolylineSampler.SampleContinuous(last.Points, last.Length),
			lastIndex);
	}

	public static (Coord Position, Coord Tangent) SampleAtElapsed(
		IReadOnlyList<RouteSegment> segments,
		double elapsedTime,
		double baseSpeed)
	{
		var sample = SampleContinuousAtElapsed(segments, elapsedTime, baseSpeed);
		return sample.Route.ToRoundedCoord();
	}
}
