using GrimSpace.Tests.World.StarSystem.Traffic;
using GrimSpace.World.StarSystem.FleetSpawner;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.Tests.World.StarSystem.FleetSpawner;

[StarSystemTestSuite]
public sealed class RandomAreaFleetSpawnRecipeTests(StarMapFixture maps)
{
	[Fact]
	public void FirstAmbientPirateBatchAtInitialCadence_HasZeroAggression()
	{
		var map = maps.Fresh(7);
		using var orchestrator = StarSystemTestHarness.CreateOrchestrator(map);

		for (var i = 0; i < FleetSpawnerConfig.DefaultCadenceTicks - 1; i++)
			orchestrator.AdvanceTick();

		var initialAmbientPirates = map.FleetRegistry.All
			.Where(fleet => fleet.State.SpawnerSource == EFleetSpawnerSource.RandomArea)
			.ToArray();

		Assert.Equal(FleetSpawnerConfig.DefaultRandomAreaTargetCount, initialAmbientPirates.Length);
		Assert.All(initialAmbientPirates, fleet => Assert.Equal(0, fleet.State.AggressionRating));
	}
}
