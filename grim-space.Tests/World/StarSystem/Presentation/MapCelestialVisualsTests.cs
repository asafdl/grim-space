using GrimSpace.World.StarSystem.Presentation.Atmosphere;

namespace GrimSpace.Tests.World.StarSystem.Presentation;

[StarSystemTestSuite]
public sealed class MapCelestialVisualsTests
{
	[Fact]
	public void AssignPlanetVariants_IsUniqueAndIndependentOfPoiOrder()
	{
		string[] ids = ["refinery", "admin", "colony", "outpost", "relay", "station"];

		for (var seed = 0; seed < 16; seed++)
		{
			var variants = MapCelestialVisuals.AssignPlanetVariants(seed, ids);
			var reordered = MapCelestialVisuals.AssignPlanetVariants(seed, ids.Reverse());

			Assert.Equal(ids.Length, variants.Values.Distinct().Count());
			foreach (var id in ids)
				Assert.Equal(variants[id], reordered[id]);
		}
	}

	[Fact]
	public void AssignPlanetVariants_RejectsMorePoisThanAvailableBodies()
	{
		var ids = Enumerable.Range(0, 7).Select(index => $"poi-{index}");

		Assert.Throws<InvalidOperationException>(() =>
			MapCelestialVisuals.AssignPlanetVariants(42, ids));
	}
}
