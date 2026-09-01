using GrimSpace.Math.Grid;

namespace GrimSpace.Math.Routes;

public static class PiecewiseRouteSampler
{
	public static double TimeRequired(IReadOnlyList<RouteSegment> segments, double baseSpeed) =>
		segments.Sum(segment => segment.Length / (baseSpeed * segment.SpeedMultiplier));

	public static (Coord Position, Coord Tangent) SampleAtElapsed(
		IReadOnlyList<RouteSegment> segments,
		double elapsedTime,
		double baseSpeed)
	{
		var remaining = elapsedTime;
		foreach (var segment in segments)
		{
			var timeForSegment = segment.Length / (baseSpeed * segment.SpeedMultiplier);
			if (remaining < timeForSegment)
			{
				var progress = remaining * baseSpeed * segment.SpeedMultiplier;
				return PolylineSampler.Sample(segment.Points, progress);
			}

			remaining -= timeForSegment;
		}

		var last = segments[^1];
		return PolylineSampler.Sample(last.Points, last.Length);
	}
}
