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
			shipToPilot);

		Assert.All(
			encounter.Spawns.Where(spawn => spawn.Team == ETeam.Player),
			spawn =>
			{
				Assert.NotNull(spawn.Pilot);
				Assert.Equal(shipToPilot[spawn.Ship.Id].PilotId, spawn.Pilot!.PilotId);
				Assert.Equal(
					shipToPilot[spawn.Ship.Id].SkillIds,
					spawn.Pilot!.SkillIds);
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
		var encounter = EngagementBattleFactory.Create(
			[playerFleet],
			run.ShipRegistry,
			seed: 3,
			id: "pilot-save-test",
			shipToPilot);
		var expectedPilot = Assert.Single(encounter.Spawns).Pilot;

		var dto = SaveDtoMapper.CaptureBattleEncounter(encounter);
		var restored = SaveDtoMapper.RestoreBattleEncounter(dto);

		Assert.Equal(expectedPilot, Assert.Single(restored.Spawns).Pilot);
	}

	[Fact]
	public void LegacyEncounterSpawnDto_RestoresPilotIdWithEmptySkills()
	{
		var dto = new BattleEncounterSpawnDto(
			Ship: SaveDtoMapper.CaptureShip(ShipInstance.FromCatalog("fighter-a", EType.Fighter)),
			Pilot: null,
			PilotId: "pilot-legacy",
			Team: ETeam.Player,
			Position: Coord.Zero,
			Fore: Coord.Forward,
			Dorsal: Coord.Up,
			AgentKind: EBattleAgentKind.Player);

		var spawn = SaveDtoMapper.RestoreBattleEncounter(
			new BattleEncounterSaveDto(
				1,
				"legacy",
				EObjective.EliminateOpponents,
				[dto],
				[])).Spawns[0];

		Assert.Equal(new PilotBattleSnapshot("pilot-legacy", []), spawn.Pilot);
	}

	[Fact]
	public void MultiShipEngagement_EachPlayerSpawnGetsDistinctPilotSnapshot()
	{
		using var run = RunState.CreateNewRun(42);
		var secondId = TypedIdGenerator.NextId("gunship");
		Assert.True(run.TryEnlistPlayerShip(new ShipSpawnDeclaration(secondId, BattleUnitType.Gunship)));
		var shipToPilot = BuildShipToPilotSnapshot(run);

		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		var encounter = EngagementBattleFactory.Create(
			[playerFleet],
			run.ShipRegistry,
			seed: 11,
			id: "multi-pilot-test",
			shipToPilot);

		var playerSpawns = encounter.Spawns.Where(spawn => spawn.Team == ETeam.Player).ToArray();
		Assert.Equal(2, playerSpawns.Length);
		Assert.Equal(2, playerSpawns.Select(spawn => spawn.Pilot!.PilotId).Distinct(StringComparer.Ordinal).Count());
	}

	[Fact]
	public void FactoryCreate_AppliesPilotSkillsFromSpawnSnapshot()
	{
		var skills = new[] { EPilotSkillId.ShieldsUp };
		var spawn = new BattleSpawn
		{
			Ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter),
			Pilot = new PilotBattleSnapshot("pilot-1", skills),
			Team = ETeam.Player,
			Position = Coord.Zero,
			ExecutionAgent = new GrimSpace.Battle.Player.UserExecutionAgent(),
		};

		var unit = Factory.Create(spawn);

		Assert.Equal(skills, unit.State.PilotSkills);
	}

	private static Dictionary<string, PilotBattleSnapshot> BuildShipToPilotSnapshot(RunState run)
	{
		var map = new Dictionary<string, PilotBattleSnapshot>(StringComparer.Ordinal);
		foreach (var shipId in run.PlayerParty.ShipIds)
		{
			Assert.True(run.PilotRegistry.TryGetForShip(shipId, out var pilot));
			map[shipId] = new PilotBattleSnapshot(
				pilot.Id,
				PilotSkillAvailability.LearnedBattleAbilities(pilot));
		}

		return map;
	}
}
