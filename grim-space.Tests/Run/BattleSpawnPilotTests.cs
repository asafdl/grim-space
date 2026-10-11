using GrimSpace.Battle;
using GrimSpace.Battle.Encounter;
using GrimSpace.Core.Ids;
using GrimSpace.Run;
using GrimSpace.Run.Persistence;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Math.Grid;
using GrimSpace.World.Factions;
using GrimSpace.Tests.World.StarSystem.Traffic;
using BattleUnitType = GrimSpace.Units.Enums.EType;

namespace GrimSpace.Tests.Run;

[IntegrationTestSuite]
public sealed class BattleSpawnPilotTests
{
	[Fact]
	public void Engagement_Create_AssignsPilotIdToPlayerSpawnsOnly()
	{
		using var run = State.CreateNewRun(42);
		var shipToPilotId = BuildShipToPilotIdMap(run);
		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(State.PlayerFleetUnitId);
		var enemyFleet = StarSystemTestHarness.CreatePirateFleet(
			"pirate-test",
			new Coord(4, 0, 0),
			EFaction.Pirates);
		foreach (var declaration in enemyFleet.Registrations)
			run.ShipRegistry.Register(declaration);

		var encounter = EngagementBattleFactory.Create(
			[playerFleet, enemyFleet],
			run.ShipRegistry,
			seed: 7,
			id: "pilot-snapshot-test",
			shipToPilotId);

		Assert.All(
			encounter.Spawns.Where(spawn => spawn.Team == ETeam.Player),
			spawn =>
			{
				Assert.NotNull(spawn.PilotId);
				Assert.Equal(shipToPilotId[spawn.Ship.Id], spawn.PilotId);
			});
		Assert.All(
			encounter.Spawns.Where(spawn => spawn.Team == ETeam.Enemy),
			spawn => Assert.Null(spawn.PilotId));
	}

	[Fact]
	public void BattleEncounterSave_RoundTripsPilotId()
	{
		using var run = State.CreateNewRun(42);
		var shipToPilotId = BuildShipToPilotIdMap(run);
		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(State.PlayerFleetUnitId);
		var encounter = EngagementBattleFactory.Create(
			[playerFleet],
			run.ShipRegistry,
			seed: 3,
			id: "pilot-save-test",
			shipToPilotId);
		var expectedPilotId = Assert.Single(encounter.Spawns).PilotId;

		var dto = SaveDtoMapper.CaptureBattleEncounter(encounter);
		var restored = SaveDtoMapper.RestoreBattleEncounter(dto);

		Assert.Equal(expectedPilotId, Assert.Single(restored.Spawns).PilotId);
	}

	[Fact]
	public void MultiShipEngagement_EachPlayerSpawnGetsDistinctPilotId()
	{
		using var run = State.CreateNewRun(42);
		var secondId = TypedIdGenerator.NextId("gunship");
		Assert.True(run.TryEnlistPlayerShip(new ShipSpawnDeclaration(secondId, BattleUnitType.Gunship)));
		var shipToPilotId = BuildShipToPilotIdMap(run);

		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(State.PlayerFleetUnitId);
		var encounter = EngagementBattleFactory.Create(
			[playerFleet],
			run.ShipRegistry,
			seed: 11,
			id: "multi-pilot-test",
			shipToPilotId);

		var playerSpawns = encounter.Spawns.Where(spawn => spawn.Team == ETeam.Player).ToArray();
		Assert.Equal(2, playerSpawns.Length);
		Assert.Equal(2, playerSpawns.Select(spawn => spawn.PilotId).Distinct(StringComparer.Ordinal).Count());
	}

	private static Dictionary<string, string> BuildShipToPilotIdMap(State run)
	{
		var map = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (var shipId in run.PlayerParty.ShipIds)
		{
			Assert.True(run.PilotRegistry.TryGetForShip(shipId, out var pilot));
			map[shipId] = pilot.Id;
		}

		return map;
	}
}
