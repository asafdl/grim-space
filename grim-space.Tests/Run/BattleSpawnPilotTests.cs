using GrimSpace.Battle;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Ids;
using GrimSpace.Pilots;
using GrimSpace.Run;
using RunState = GrimSpace.Run.State;
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
	public void Engagement_Create_AssignsPilotSnapshotToPlayerSpawnsOnly()
	{
		using var run = RunState.CreateNewRun(42);
		var shipToPilot = BuildShipToPilotSnapshot(run);
		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		var enemyFleet = RegisterEnemyFleet(run, "pirate-test", new Coord(4, 0, 0));

		var encounter = EngagementBattleFactory.Create(
			[playerFleet, enemyFleet],
			run.ItemInventory,
			seed: 7,
			id: "pilot-snapshot-test",
			initiatorFleetId: playerFleet.State.Id,
			shipIdToPilotSnapshot: shipToPilot);

		Assert.All(
			encounter.Spawns.Where(spawn => spawn.Team == ETeam.Player),
			spawn =>
			{
				Assert.NotNull(spawn.Pilot);
				Assert.Equal(shipToPilot[spawn.Ship.Id].PilotId, spawn.Pilot!.PilotId);
				Assert.Equal(
					shipToPilot[spawn.Ship.Id].CapabilityGrants,
					spawn.Pilot!.CapabilityGrants);
			});
		Assert.All(
			encounter.Spawns.Where(spawn => spawn.Team == ETeam.Enemy),
			spawn => Assert.Null(spawn.Pilot));
	}

	[Fact]
	public void BattleEncounterSave_RoundTripsPilotSnapshot()
	{
		using var run = RunState.CreateNewRun(42);
		var shipToPilot = BuildShipToPilotSnapshot(run);
		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		var enemyFleet = RegisterEnemyFleet(run, "pirate-save-test", new Coord(4, 0, 0));
		var encounter = EngagementBattleFactory.Create(
			[playerFleet, enemyFleet],
			run.ItemInventory,
			seed: 3,
			id: "pilot-save-test",
			initiatorFleetId: playerFleet.State.Id,
			shipIdToPilotSnapshot: shipToPilot);
		var expectedPilot = Assert.Single(encounter.Spawns, spawn => spawn.Team == ETeam.Player).Pilot;

		var dto = SaveDtoMapper.CaptureBattleEncounter(encounter);
		var restored = SaveDtoMapper.RestoreBattleEncounter(dto);

		var restoredPilot = Assert.Single(
			restored.Spawns,
			spawn => spawn.Team == ETeam.Player).Pilot;
		Assert.NotNull(expectedPilot);
		Assert.NotNull(restoredPilot);
		Assert.Equal(expectedPilot.PilotId, restoredPilot.PilotId);
		Assert.Equal(expectedPilot.PortraitId, restoredPilot.PortraitId);
		Assert.Equal(expectedPilot.CapabilityGrants, restoredPilot.CapabilityGrants);
	}

	[Fact]
	public void MultiShipEngagement_EachPlayerSpawnGetsDistinctPilotSnapshot()
	{
		using var run = RunState.CreateNewRun(42);
		var secondId = TypedIdGenerator.NextId("gunship");
		Assert.True(run.TryEnlistPlayerShip(new ShipSpawnDeclaration(secondId, BattleUnitType.Gunship)));
		var shipToPilot = BuildShipToPilotSnapshot(run);

		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		var enemyFleet = RegisterEnemyFleet(run, "pirate-multi-pilot", new Coord(4, 0, 0));
		var encounter = EngagementBattleFactory.Create(
			[playerFleet, enemyFleet],
			run.ItemInventory,
			seed: 11,
			id: "multi-pilot-test",
			initiatorFleetId: playerFleet.State.Id,
			shipIdToPilotSnapshot: shipToPilot);

		var playerSpawns = encounter.Spawns.Where(spawn => spawn.Team == ETeam.Player).ToArray();
		Assert.Equal(2, playerSpawns.Length);
		Assert.Equal(2, playerSpawns.Select(spawn => spawn.Pilot!.PilotId).Distinct(StringComparer.Ordinal).Count());
	}

	[Fact]
	public void FromEncounter_AppliesPilotRuntimeFromSpawnSnapshot()
	{
		var skills = new[] { EPilotSkillId.ShieldsUp };
		var spawn = new BattleSpawn
		{
			Ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter),
			Pilot = PilotBattleSnapshot.FromSkills("pilot-1", skills),
			Team = ETeam.Player,
			Position = Coord.Zero,
			ExecutionAgent = new GrimSpace.Battle.Player.UserExecutionAgent(),
		};
		var encounter = new BattleEncounter
		{
			Seed = 1,
			Id = "pilot-runtime",
			Objective = GrimSpace.Battle.Objectives.EObjective.EliminateOpponents,
			Spawns =
			[
				spawn,
				new BattleSpawn
				{
					Ship = ShipInstance.FromCatalog("enemy", EType.Fighter),
					Team = ETeam.Enemy,
					Position = new Coord(0, 0, 0),
					ExecutionAgent = BattleAgentFactory.Create(EBattleAgentKind.Ai),
				},
			],
		};

		using var battle = BattleOrchestrator.FromEncounter(encounter, gridSize: 12);
		var runtime = battle.Engine.ActorRuntimes.For("fighter-a");

		Assert.Equal(
			[EPilotCapabilityId.ShieldBoost],
			runtime.PilotCapabilities.OrderBy(capability => capability).ToArray());
	}

	private static GrimSpace.World.StarSystem.Units.Fleet RegisterEnemyFleet(
		RunState run,
		string fleetId,
		Coord position)
	{
		var enemyFleet = StarSystemTestHarness.CreatePirateFleet(fleetId, position, EFaction.Pirates);
		foreach (var declaration in enemyFleet.Registrations)
			run.ItemInventory.Register(declaration, enemyFleet.State.Id);
		return enemyFleet;
	}

	private static Dictionary<string, PilotBattleSnapshot> BuildShipToPilotSnapshot(RunState run)
	{
		var map = new Dictionary<string, PilotBattleSnapshot>(StringComparer.Ordinal);
		foreach (var shipId in run.GetFieldedShipIds())
		{
			Assert.True(run.PilotRegistry.TryGetForShip(shipId, out var pilot));
			map[shipId] = PilotBattleSnapshot.FromSkills(
				pilot.Id,
				pilot.LearnedSkills);
		}

		return map;
	}
}
