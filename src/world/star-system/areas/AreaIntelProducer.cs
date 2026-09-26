using GrimSpace.Math.Grid;
using GrimSpace.Math.Routes;

namespace GrimSpace.World.StarSystem.Areas;

public static class AreaIntelProducer
{
	private const double NearFractionOfMap = 0.1;
	private const double BetweenFractionOfMap = 0.15;

	public static (string ClosestId, string SecondClosestId, string ThirdClosestId) OrderLandmarksByDistanceFromCenter(
		Coord center,
		string landmarkAId,
		string landmarkBId,
		string landmarkCId,
		Func<string, Coord> resolvePosition)
	{
		ArgumentNullException.ThrowIfNull(resolvePosition);

		var ordered = new[] { landmarkAId, landmarkBId, landmarkCId }
			.Select(id => (Id: id, Distance: RouteGeometry.Distance(center, resolvePosition(id))))
			.OrderBy(entry => entry.Distance)
			.ToArray();

		return (ordered[0].Id, ordered[1].Id, ordered[2].Id);
	}

	public static AreaIntel Produce(
		AreaIntelContext context,
		Coord searchPoint,
		IReadOnlyList<string> referenceIds,
		Func<string, Coord> resolvePosition,
		int mapSize)
	{
		ArgumentNullException.ThrowIfNull(context);
		ArgumentNullException.ThrowIfNull(referenceIds);
		ArgumentNullException.ThrowIfNull(resolvePosition);
		ArgumentException.ThrowIfNullOrEmpty(context.LandmarkAId);
		ArgumentException.ThrowIfNullOrEmpty(context.LandmarkBId);
		ArgumentException.ThrowIfNullOrEmpty(context.LandmarkCId);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(mapSize);
		if (referenceIds.Count == 0)
			throw new ArgumentException("At least one reference is required.", nameof(referenceIds));

		var references = referenceIds
			.Select(id => (Id: id, Position: resolvePosition(id)))
			.OrderBy(reference => RouteGeometry.Distance(searchPoint, reference.Position))
			.ToArray();
		var navigationReferences = references
			.Where(reference => !AreaBorderAnchor.TryParseId(reference.Id, out _))
			.ToArray();
		if (navigationReferences.Length == 0)
			throw new InvalidOperationException("At least one navigation landmark reference is required.");

		var closestNavigation = navigationReferences[0];
		if (RouteGeometry.Distance(searchPoint, closestNavigation.Position) <= mapSize * NearFractionOfMap)
		{
			return new AreaIntel(
				"Somewhere near {A}.",
				closestNavigation.Id,
				context.LandmarkBId,
				context.LandmarkCId);
		}

		(string FirstId, string SecondId, double Distance)? bestPair = null;
		for (var i = 0; i < navigationReferences.Length; i++)
		{
			for (var j = i + 1; j < navigationReferences.Length; j++)
			{
				var first = navigationReferences[i];
				var second = navigationReferences[j];
				var distance = RouteGeometry.PointToSegmentDistance(searchPoint, first.Position, second.Position);
				if (distance <= mapSize * BetweenFractionOfMap
					&& (bestPair is null || distance < bestPair.Value.Distance))
					bestPair = (first.Id, second.Id, distance);
			}
		}
		if (bestPair is { } pair)
			return new AreaIntel("Somewhere between {A} and {B}.", pair.FirstId, pair.SecondId, context.LandmarkCId);

		var landmarkIds = new List<string>();
		foreach (var id in new[] { context.LandmarkAId, context.LandmarkBId, context.LandmarkCId })
		{
			if (AreaBorderAnchor.TryParseId(id, out _))
				continue;
			if (!landmarkIds.Contains(id, StringComparer.Ordinal))
				landmarkIds.Add(id);
		}

		foreach (var reference in navigationReferences)
		{
			if (landmarkIds.Count >= 3)
				break;
			if (!landmarkIds.Contains(reference.Id, StringComparer.Ordinal))
				landmarkIds.Add(reference.Id);
		}

		if (landmarkIds.Count >= 3)
		{
			return new AreaIntel(
				"Somewhere in the general area between {A}, {B}, and {C}.",
				landmarkIds[0],
				landmarkIds[1],
				landmarkIds[2]);
		}

		if (landmarkIds.Count == 2)
		{
			return new AreaIntel(
				"Somewhere between {A} and {B}.",
				landmarkIds[0],
				landmarkIds[1],
				context.LandmarkCId);
		}

		return new AreaIntel(
			"Somewhere near {A}.",
			landmarkIds[0],
			context.LandmarkBId,
			context.LandmarkCId);
	}
}
