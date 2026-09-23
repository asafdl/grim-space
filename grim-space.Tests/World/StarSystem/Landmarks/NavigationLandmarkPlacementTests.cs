using GrimSpace.Math.Grid;
using GrimSpace.Math.Routes;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Landmarks;

namespace GrimSpace.Tests.World.StarSystem.Landmarks;

[StarSystemTestSuite]
public sealed class NavigationLandmarkPlacementTests
{
	[Fact]
	public void GeneratedLandmarks_RespectClearanceFromPoisRoutesAndEachOther()
	{
		var map = StarMap.Create(101);
		var profile = map.Blueprint.NavigationLandmarkProfile;

		foreach (var landmark in map.NavigationLandmarks)
		{
			Assert.True(
				GridBounds.IsCircleWhollyInRectangle(landmark.Position, landmark.Radius, map.Width, map.Height));

			foreach (var poi in map.PointsOfInterest)
			{
				var clearance = poi.RouteExclusionRadius + profile.PoiClearance + landmark.Radius;
				var dx = landmark.Position.X - poi.PlacedCenter.X;
				var dz = landmark.Position.Z - poi.PlacedCenter.Z;
				Assert.True(dx * (long)dx + dz * (long)dz >= (long)clearance * clearance);
			}

			foreach (var route in map.RoutesById.Values)
			{
				var distance = RouteGeometry.PointToPolylineDistance(landmark.Position, route.Centerline);
				var clearance = route.HalfWidth + profile.RouteClearance + landmark.Radius;
				Assert.True(distance >= clearance);
			}
		}

		for (var i = 0; i < map.NavigationLandmarks.Count; i++)
		{
			for (var j = i + 1; j < map.NavigationLandmarks.Count; j++)
			{
				var a = map.NavigationLandmarks[i];
				var b = map.NavigationLandmarks[j];
				var separation = a.Radius + profile.LandmarkSeparation + b.Radius;
				var dx = a.Position.X - b.Position.X;
				var dz = a.Position.Z - b.Position.Z;
				Assert.True(dx * (long)dx + dz * (long)dz >= (long)separation * separation);
			}
		}
	}
}
