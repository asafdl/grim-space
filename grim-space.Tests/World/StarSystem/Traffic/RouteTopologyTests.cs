using GrimSpace.Math.Grid;
using GrimSpace.Math.Routes;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Poi.Concrete;
using GrimSpace.World.StarSystem.Traffic;

namespace GrimSpace.Tests.World.StarSystem.Traffic;

public sealed class RouteTopologyTests
{
	[Fact]
	public void CreateDevDefault_HasFourDocks_NoStarDock()
	{
		var world = StarMap.CreateDevDefault(0);

		Assert.Equal(4, world.DocksById.Count);
		Assert.DoesNotContain(
			world.PointsOfInterest.First(p => p is Star).Id,
			world.DocksByPoiId.Keys);
	}

	[Fact]
	public void CreateDevDefault_BuildsThreeSupplyChainRoutes()
	{
		var world = StarMap.CreateDevDefault(99);
		var plan = world.Blueprint.SupplyPlan;

		Assert.Equal(3, world.RoutesById.Count);
		foreach (var (fromPoiId, toPoiId) in plan.RouteConnections)
		{
			var fromDock = world.DocksByPoiId[fromPoiId].Id;
			var toDock = world.DocksByPoiId[toPoiId].Id;
			var routeId = string.Compare(fromDock, toDock, StringComparison.Ordinal) <= 0
				? $"route:{fromDock}:{toDock}"
				: $"route:{toDock}:{fromDock}";
			Assert.Contains(routeId, world.RoutesById.Keys);
		}
	}

	[Fact]
	public void Routes_StartAndEndAtDockPositions()
	{
		var world = StarMap.CreateDevDefault(11);
		foreach (var route in world.RoutesById.Values)
		{
			var dockA = world.DocksById[route.DockAId];
			var dockB = world.DocksById[route.DockBId];

			Assert.Equal(dockA.Position, route.Centerline[0]);
			Assert.Equal(dockB.Position, route.Centerline[^1]);
		}
	}

	[Fact]
	public void Routes_HavePositiveHalfWidth()
	{
		var world = StarMap.CreateDevDefault(5);
		foreach (var route in world.RoutesById.Values)
			Assert.True(route.HalfWidth > 0);
	}

	[Fact]
	public void Routes_HaveAtLeastTwoDistinctCenterlinePoints()
	{
		var world = StarMap.CreateDevDefault(5);
		foreach (var route in world.RoutesById.Values)
		{
			Assert.True(route.Centerline.Count >= 2);
			Assert.NotEqual(route.Centerline[0], route.Centerline[^1]);
		}
	}

	[Fact]
	public void Routes_UseEuclideanPolylineLength()
	{
		var world = StarMap.CreateDevDefault(5);
		foreach (var route in world.RoutesById.Values)
		{
			var expected = route.Centerline.Zip(route.Centerline.Skip(1))
				.Sum(pair =>
				{
					var dx = pair.Second.X - pair.First.X;
					var dz = pair.Second.Z - pair.First.Z;
					return System.Math.Sqrt(dx * dx + dz * dz);
				});

			Assert.Equal(expected, route.Length, precision: 5);
		}
	}

	[Fact]
	public void Topology_IsDeterministicForSeed()
	{
		var docks = new[]
		{
			new Dock("dock-a", "poi-a", new Coord(200, 0, 200)),
			new Dock("dock-b", "poi-b", new Coord(800, 0, 800)),
		};

		var first = RouteBuilder.Build(
			123,
			1024,
			1024,
			docks,
			[new RoutePair("dock-a", "dock-b")],
			[],
			StarMap.DevRouteHalfWidth);
		var second = RouteBuilder.Build(
			123,
			1024,
			1024,
			docks,
			[new RoutePair("dock-a", "dock-b")],
			[],
			StarMap.DevRouteHalfWidth);

		Assert.Equal(first.Keys.OrderBy(id => id), second.Keys.OrderBy(id => id));
		foreach (var routeId in first.Keys)
		{
			var a = first[routeId];
			var b = second[routeId];
			Assert.Equal(a.Centerline, b.Centerline);
			Assert.Equal(a.HalfWidth, b.HalfWidth);
		}
	}

	[Fact]
	public void ReversedRoutePair_DoesNotChangeGeometryOrCreateSecondRoute()
	{
		var docks = new[]
		{
			new Dock("dock-a", "poi-a", new Coord(100, 0, 100)),
			new Dock("dock-b", "poi-b", new Coord(900, 0, 900)),
		};

		var forward = RouteBuilder.Build(
			42,
			1024,
			1024,
			docks,
			[new RoutePair("dock-a", "dock-b")],
			[],
			StarMap.DevRouteHalfWidth);

		var reversed = RouteBuilder.Build(
			42,
			1024,
			1024,
			docks,
			[new RoutePair("dock-b", "dock-a")],
			[],
			StarMap.DevRouteHalfWidth);

		Assert.Single(forward);
		Assert.Single(reversed);
		Assert.Equal(forward.Keys.Single(), reversed.Keys.Single());
		Assert.Equal(forward.Values.Single().Centerline, reversed.Values.Single().Centerline);
	}

	[Fact]
	public void DuplicateReversePairs_AreRejected()
	{
		var docks = new[]
		{
			new Dock("dock-a", "poi-a", new Coord(100, 0, 100)),
			new Dock("dock-b", "poi-b", new Coord(900, 0, 900)),
		};

		Assert.Throws<ArgumentException>(() => RouteBuilder.Build(
			42,
			1024,
			1024,
			docks,
			[
				new RoutePair("dock-a", "dock-b"),
				new RoutePair("dock-b", "dock-a"),
			],
			[],
			StarMap.DevRouteHalfWidth));
	}

	[Fact]
	public void Corridors_StayInsideMapBounds()
	{
		var world = StarMap.CreateDevDefault(21);
		foreach (var route in world.RoutesById.Values)
		{
			foreach (var point in CorridorSamplePoints(route))
				Assert.True(world.IsInBounds(point));
		}
	}

	[Fact]
	public void Corridors_ClearCircularExclusions()
	{
		var world = StarMap.CreateDevDefault(21);
		var star = world.PointsOfInterest.First(p => p is Star);
		var exclusion = new CircularExclusion(star.PlacedCenter, star.RouteExclusionRadius);

		foreach (var route in world.RoutesById.Values)
		{
			var forbiddenDistance = exclusion.Radius + route.HalfWidth + 6;
			for (var i = 1; i < route.Centerline.Count; i++)
			{
				var distance = RouteGeometry.PointToSegmentDistance(
					exclusion.Center,
					route.Centerline[i - 1],
					route.Centerline[i]);
				Assert.True(distance >= forbiddenDistance);
			}
		}
	}

	[Fact]
	public void CrossingRoutes_AreGeneratedWithoutRejectionOrSplitting()
	{
		var docks = new[]
		{
			new Dock("dock-a", "poi-a", new Coord(200, 0, 500)),
			new Dock("dock-b", "poi-b", new Coord(800, 0, 500)),
			new Dock("dock-c", "poi-c", new Coord(500, 0, 200)),
			new Dock("dock-d", "poi-d", new Coord(500, 0, 800)),
		};

		var routes = RouteBuilder.Build(
			77,
			1024,
			1024,
			docks,
			[
				new RoutePair("dock-a", "dock-b"),
				new RoutePair("dock-c", "dock-d"),
			],
			[],
			StarMap.DevRouteHalfWidth);

		Assert.Equal(2, routes.Count);
		Assert.True(RoutesCross(routes.Values.ToArray()));
	}

	private static bool RoutesCross(IReadOnlyList<SpaceRoute> routes)
	{
		var left = routes[0].Centerline;
		var right = routes[1].Centerline;

		for (var i = 1; i < left.Count; i++)
		{
			for (var j = 1; j < right.Count; j++)
			{
				if (RouteGeometry.SegmentsIntersect(left[i - 1], left[i], right[j - 1], right[j]))
					return true;
			}
		}

		return false;
	}

	private static IEnumerable<Coord> CorridorSamplePoints(SpaceRoute route)
	{
		for (var i = 0; i < route.Centerline.Count; i++)
		{
			var tangent = CorridorTangent(route.Centerline, i);
			var perpendicular = (-tangent.Z, tangent.X);
			yield return Offset(route.Centerline[i], perpendicular, route.HalfWidth);
			yield return Offset(route.Centerline[i], perpendicular, -route.HalfWidth);
			yield return route.Centerline[i];
		}
	}

	private static (double X, double Z) CorridorTangent(IReadOnlyList<Coord> centerline, int index)
	{
		if (centerline.Count < 2)
			return (1.0, 0.0);

		var previous = centerline[System.Math.Max(0, index - 1)];
		var next = centerline[System.Math.Min(centerline.Count - 1, index + 1)];
		return RouteGeometry.UnitVector(next.X - previous.X, next.Z - previous.Z);
	}

	private static Coord Offset(Coord origin, (double X, double Z) direction, double distance) =>
		new(
			(int)System.Math.Round(origin.X + direction.X * distance),
			0,
			(int)System.Math.Round(origin.Z + direction.Z * distance));
}
