using GrimSpace.Math.Grid;
using GrimSpace.Math.Routes;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Landmarks;

namespace GrimSpace.Tests.World.StarSystem.Landmarks;

[StarSystemTestSuite]
public sealed class NavigationLandmarkGeneratorTests
{
	[Fact]
	public void Generate_SameSeedAndLayout_ProducesIdenticalLandmarks()
	{
		var blueprint = SupplySystemGenerator.CreateBlueprint(4242);
		var first = GenerateLandmarks(blueprint, 0);
		var second = GenerateLandmarks(blueprint, 0);

		Assert.Equal(
			first.Select(landmark => (landmark.Id, landmark.DisplayName, landmark.Kind, landmark.Position, landmark.Radius, landmark.VisualSeed)),
			second.Select(landmark => (landmark.Id, landmark.DisplayName, landmark.Kind, landmark.Position, landmark.Radius, landmark.VisualSeed)));
	}

	[Fact]
	public void Generate_DifferentSeeds_ProduceVariation()
	{
		var a = StarMap.Create(1).NavigationLandmarks;
		var b = StarMap.Create(2).NavigationLandmarks;

		Assert.NotEqual(
			a.Select(landmark => landmark.Position),
			b.Select(landmark => landmark.Position));
	}

	[Fact]
	public void Generate_DefaultSupplyProfile_CountWithinBoundsAndPoolsRepresented()
	{
		var map = StarMap.Create(77);
		var landmarks = map.NavigationLandmarks;
		var profile = map.Blueprint.NavigationLandmarkProfile;

		Assert.InRange(landmarks.Count, profile.MinimumCount, profile.MaximumCount);
		Assert.Contains(landmarks, landmark => GenericNavigationLandmarkCatalog.Entries.Any(entry => entry.Kind == landmark.Kind));
		Assert.Contains(landmarks, landmark => CopperNavigationLandmarkCatalog.Entries.Any(entry => entry.Kind == landmark.Kind));
	}

	[Fact]
	public void Generate_NoIdCollisionsWithWorldObjects()
	{
		var map = StarMap.Create(88);
		var reserved = map.PointsOfInterest.Select(poi => poi.Id)
			.Concat(map.DocksById.Keys)
			.ToHashSet(StringComparer.Ordinal);

		foreach (var landmark in map.NavigationLandmarks)
			Assert.DoesNotContain(landmark.Id, reserved);
	}

	[Fact]
	public void StarMap_Fork_PreservesLandmarks()
	{
		var map = StarMap.Create(5);
		var fork = map.Fork();

		Assert.Equal(map.NavigationLandmarks, fork.NavigationLandmarks);
		Assert.Same(map.NavigationLandmarks, fork.NavigationLandmarks);
	}

	private static IReadOnlyList<NavigationLandmark> GenerateLandmarks(StarSystemBlueprint blueprint, int layoutAttempt)
	{
		var map = StarMap.Create(blueprint.Seed);
		return map.NavigationLandmarks;
	}
}
