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
		var closest = references[0];
		if (RouteGeometry.Distance(searchPoint, closest.Position) <= mapSize * NearFractionOfMap)
			return new AreaIntel("Somewhere near {A}.", closest.Id, context.LandmarkBId, context.LandmarkCId);

		(string FirstId, string SecondId, double Distance)? bestPair = null;
		for (var i = 0; i < references.Length; i++)
		{
			for (var j = i + 1; j < references.Length; j++)
			{
				var first = references[i];
				var second = references[j];
				if (AreaBorderAnchor.TryParseId(first.Id, out _)
					&& AreaBorderAnchor.TryParseId(second.Id, out _))
					continue;

				var distance = RouteGeometry.PointToSegmentDistance(searchPoint, first.Position, second.Position);
				if (distance <= mapSize * BetweenFractionOfMap
					&& (bestPair is null || distance < bestPair.Value.Distance))
					bestPair = (first.Id, second.Id, distance);
			}
		}
		if (bestPair is { } pair)
			return new AreaIntel("Somewhere between {A} and {B}.", pair.FirstId, pair.SecondId, context.LandmarkCId);

		var triangleIds = new[] { context.LandmarkAId, context.LandmarkBId, context.LandmarkCId };
		var borderCount = triangleIds.Count(id => AreaBorderAnchor.TryParseId(id, out _));
		if (borderCount == 2)
		{
			var landmarkId = triangleIds.Single(id => !AreaBorderAnchor.TryParseId(id, out _));
			return new AreaIntel(
				"Somewhere in the general area between {A} and the sector rim.",
				landmarkId,
				context.LandmarkBId,
				context.LandmarkCId);
		}

		return new AreaIntel(
			"Somewhere in the general area between {A}, {B}, and {C}.",
			context.LandmarkAId,
			context.LandmarkBId,
			context.LandmarkCId);
	}
}
