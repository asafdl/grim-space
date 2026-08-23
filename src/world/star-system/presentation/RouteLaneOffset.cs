using GrimSpace.World.StarSystem.Traffic;

namespace GrimSpace.World.StarSystem.Presentation;

internal static class RouteLaneOffset
{
	private const double LaneMargin = 4;
	private const double MergeFraction = 0.14;

	// Normalized lateral slots within the corridor (no center lane).
	private static readonly double[] LaneSlots = [-0.70, -0.38, 0.38, 0.70];

	public static double Compute(string unitId, SpaceRoute route, double progress, bool towardDockB)
	{
		var routeLength = route.Length;
		if (routeLength <= 0)
			return 0;

		var maxOffset = System.Math.Max(0, route.HalfWidth - LaneMargin);
		if (maxOffset <= 0)
			return 0;

		var laneSlot = PickLaneSlot(unitId);
		var directionSign = towardDockB ? 1 : -1;
		var cruiseOffset = laneSlot * directionSign * maxOffset;

		var t = System.Math.Clamp(progress / routeLength, 0, 1);
		return cruiseOffset * EndpointBlend(t);
	}

	private static double PickLaneSlot(string unitId)
	{
		var laneIndex = (int)(StableHash(unitId) % LaneSlots.Length);
		return LaneSlots[laneIndex];
	}

	private static double EndpointBlend(double t)
	{
		if (t < MergeFraction)
			return SmoothStep(t / MergeFraction);

		if (t > 1 - MergeFraction)
			return SmoothStep((1 - t) / MergeFraction);

		return 1;
	}

	private static double SmoothStep(double t)
	{
		t = System.Math.Clamp(t, 0, 1);
		return t * t * (3 - 2 * t);
	}

	private static uint StableHash(string value)
	{
		unchecked
		{
			uint hash = 2166136261;
			foreach (var character in value)
				hash = (hash ^ character) * 16777619;
			return hash;
		}
	}
}
