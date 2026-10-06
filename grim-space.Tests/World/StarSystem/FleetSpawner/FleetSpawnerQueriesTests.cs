using GrimSpace.Math.Grid;
using GrimSpace.Run.Persistence;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.FleetSpawner;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem.Traffic;

namespace GrimSpace.Tests.World.StarSystem.FleetSpawner;

[StarSystemTestSuite]
public sealed class FleetSpawnerQueriesTests(StarMapFixture maps)
{
	[Fact]
	public void DueForExpiry_SkipsFleetsWithPursuitDirective()
	{
		var map = maps.Fresh(7);
		const int tick = 100;
		var fleet = StarSystemTestHarness.CreatePirateFleet("pirate-pursuit", new Coord(4, 4, 4), map.ControllingFaction);
		fleet.State.SpawnerSource = EFleetSpawnerSource.RandomArea;
		fleet.State.FleetSpawnerExpiresAtTick = tick - 1;
		fleet.State.PursuitDirective = new FleetPursuitDirective("contract-1", "player-1");
		map.FleetRegistry.Add(fleet);

		Assert.Empty(FleetSpawnerQueries.DueForExpiry(
			map.FleetRegistry,
			EFleetSpawnerSource.RandomArea,
			tick));
	}

	[Fact]
	public void CountTowardTarget_IncludesExpiredFleetInPursuit()
	{
		var map = maps.Fresh(7);
		const int tick = 100;
		var fleet = StarSystemTestHarness.CreatePirateFleet("pirate-count", new Coord(2, 2, 2), map.ControllingFaction);
		fleet.State.SpawnerSource = EFleetSpawnerSource.RandomArea;
		fleet.State.FleetSpawnerExpiresAtTick = tick - 1;
		fleet.State.PursuitDirective = new FleetPursuitDirective("contract-1", "player-1");
		map.FleetRegistry.Add(fleet);

		Assert.Equal(1, FleetSpawnerQueries.CountTowardTarget(
			map.FleetRegistry,
			EFleetSpawnerSource.RandomArea,
			tick));
	}

	[Fact]
	public void SaveDtoMapper_RoundTripsFleetPursuitDirective()
	{
		var map = maps.Fresh(3);
		var fleet = map.FleetRegistry.All.First();
		fleet.State.PursuitDirective = new FleetPursuitDirective("delivery-1", "player-fleet");
		var registry = PersistenceRegistry.CreateDefault();

		var restored = SaveDtoMapper.RestoreStarMap(
			SaveDtoMapper.CaptureStarMap(map, registry),
			registry);
		var restoredFleet = restored.FleetRegistry.FleetOf(fleet.State.Id);

		Assert.NotNull(restoredFleet.State.PursuitDirective);
		Assert.Equal("delivery-1", restoredFleet.State.PursuitDirective.ContractId);
		Assert.Equal("player-fleet", restoredFleet.State.PursuitDirective.TargetFleetId);
	}
}
