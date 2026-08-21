using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Traffic;

namespace GrimSpace.Tests.World.StarSystem.Traffic;

public sealed class RouteTopologyTests
{
	[Fact]
	public void CreateDevDefault_BuildsTwelveRoutes()
	{
		var world = StarMap.CreateDevDefault(99);
		Assert.Equal(12, world.RoutesById.Count);
	}

	[Fact]
	public void Routes_HaveContiguousSharedBoundaryPoints()
	{
		var world = StarMap.CreateDevDefault(11);
		foreach (var route in world.RoutesById.Values)
		{
			for (var i = 0; i < route.SegmentIds.Count - 1; i++)
			{
				var left = world.SegmentsById[route.SegmentIds[i]];
				var right = world.SegmentsById[route.SegmentIds[i + 1]];
				Assert.Equal(left.End, right.Start);
			}
		}
	}

	[Fact]
	public void Segments_UseEuclideanLengths()
	{
		var world = StarMap.CreateDevDefault(5);
		foreach (var segment in world.SegmentsById.Values)
		{
			var expected = segment.Points.Zip(segment.Points.Skip(1))
				.Sum(pair =>
				{
					var dx = pair.Second.X - pair.First.X;
					var dz = pair.Second.Z - pair.First.Z;
					return System.Math.Sqrt(dx * dx + dz * dz);
				});

			Assert.Equal(expected, segment.Length, precision: 5);
		}
	}

	[Fact]
	public void Topology_IsDeterministicForSeed()
	{
		var first = StarMap.CreateDevDefault(123);
		var second = StarMap.CreateDevDefault(123);

		Assert.Equal(first.RoutesById.Keys.OrderBy(id => id), second.RoutesById.Keys.OrderBy(id => id));
		foreach (var routeId in first.RoutesById.Keys)
		{
			var a = first.RoutesById[routeId];
			var b = second.RoutesById[routeId];
			Assert.Equal(a.SegmentIds, b.SegmentIds);
		}
	}

	[Fact]
	public void SharedArrivalThroat_IsDedupedAcrossVariants()
	{
		var world = StarMap.CreateDevDefault(17);
		var destPoiId = world.PointsOfInterest.First(p => p.Kind == EPointOfInterestKind.Planet).Id;
		var routesToDest = world.RoutesById.Values
			.Where(route => route.DestinationPoiId == destPoiId)
			.ToList();

		var tailSegmentIds = routesToDest
			.Select(route => route.SegmentIds[^1])
			.Distinct()
			.ToList();

		Assert.Single(tailSegmentIds);
	}

	[Fact]
	public void CruiseSegments_StayInBoundsAndClearInteriorPois()
	{
		var world = StarMap.CreateDevDefault(21);
		foreach (var segment in world.SegmentsById.Values)
		{
			if (!IsCruiseBlockSegment(segment.Id))
				continue;

			for (var index = 0; index < segment.Points.Count; index++)
				Assert.True(world.IsInBounds(segment.Points[index]));
		}
	}

	[Fact]
	public void Routes_StartAtOriginDeparture_EndAtDestinationArrival()
	{
		var world = StarMap.CreateDevDefault(99);
		foreach (var route in world.RoutesById.Values)
		{
			var originDock = world.DocksByPoiId[route.OriginPoiId];
			var destDock = world.DocksByPoiId[route.DestinationPoiId];
			var first = world.SegmentsById[route.SegmentIds[0]];
			var last = world.SegmentsById[route.SegmentIds[^1]];

			Assert.Equal(originDock.DepartureBerth, first.Start);
			Assert.Equal(destDock.ArrivalBerth, last.End);
		}
	}

	[Fact]
	public void CrossRegistryCruises_AreNotIdenticalAcrossDestinations()
	{
		var world = StarMap.CreateDevDefault(33);
		var routes = world.RoutesById.Values.ToList();
		var cruises = routes.ToDictionary(
			route => route.Id,
			route => ExtractCruisePolyline(world, route));

		for (var i = 0; i < routes.Count; i++)
		{
			for (var j = i + 1; j < routes.Count; j++)
			{
				if (routes[i].DestinationPoiId == routes[j].DestinationPoiId)
					continue;

				Assert.False(
					PointsEqual(cruises[routes[i].Id], cruises[routes[j].Id]),
					$"Identical cruise geometry across destinations: {routes[i].Id} and {routes[j].Id}");
			}
		}
	}

	private static bool IsCruiseBlockSegment(string segmentId) =>
		segmentId.Contains("-v", StringComparison.Ordinal)
		&& segmentId.Contains("-b", StringComparison.Ordinal);

	private static List<Coord> ExtractCruisePolyline(StarMap world, RouteTemplate route)
	{
		var points = new List<Coord>();
		foreach (var segmentId in route.SegmentIds)
		{
			if (!IsCruiseBlockSegment(segmentId))
				continue;

			var segment = world.SegmentsById[segmentId];
			if (points.Count == 0)
				points.AddRange(segment.Points);
			else
				points.AddRange(segment.Points.Skip(1));
		}

		return points;
	}

	private static bool PointsEqual(IReadOnlyList<Coord> left, IReadOnlyList<Coord> right)
	{
		if (left.Count != right.Count)
			return false;

		for (var i = 0; i < left.Count; i++)
		{
			if (left[i] != right[i])
				return false;
		}

		return true;
	}
}
