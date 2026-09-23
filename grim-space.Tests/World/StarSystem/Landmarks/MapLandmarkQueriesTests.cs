using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Landmarks;

namespace GrimSpace.Tests.World.StarSystem.Landmarks;

[StarSystemTestSuite]
public sealed class MapLandmarkQueriesTests
{
	[Fact]
	public void TryGet_ResolvesPoiAndNavigationLandmark()
	{
		var map = StarMap.Create(3);
		var poi = map.PointsOfInterest[0];
		var landmark = map.NavigationLandmarks[0];

		Assert.True(MapLandmarkQueries.TryGet(map, poi.Id, out var poiRef));
		Assert.Equal(EMapLandmarkSource.PointOfInterest, poiRef.Source);

		Assert.True(MapLandmarkQueries.TryGet(map, landmark.Id, out var landmarkRef));
		Assert.Equal(EMapLandmarkSource.NavigationLandmark, landmarkRef.Source);
		Assert.Equal(landmark.DisplayName, MapLandmarkQueries.GetDisplayName(map, landmark.Id));
	}
}
