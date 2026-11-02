using GrimSpace.Tests.Run;
using GrimSpace.Battle;
using GrimSpace.Battle.Presentation;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Encounter.Generation;
using GrimSpace.Run;
using GrimSpace.Tutorials;
using GrimSpace.Tests.World.StarSystem.Traffic;
using GrimSpace.Pilots;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using FleetType = GrimSpace.World.StarSystem.Units.EType;
using BattleUnitType = GrimSpace.Units.Enums.EType;
using RunState = GrimSpace.Run.State;

namespace GrimSpace.Tests.Run;

[IntegrationTestSuite]
public sealed class DeploymentPlacementTests
{
	[Fact]
	public void DevDuel_PlacesPlayerOnLowXAndEnemyOnHighX()
	{
		var (player, enemy) = DeploymentPlacement.DevDuel(
			BattleUnitType.Fighter,
			BattleUnitType.Carrier,
			seed: 42,
			gridSize: 64);

		Assert.True(player.Position.X < 64 / 2);
		Assert.True(enemy.Position.X >= 64 / 2);
		Assert.NotEqual(player.Position, enemy.Position);
	}

	[Fact]
	public void DevDuel_UsesT0CatalogLoadouts()
	{
		var (player, enemy) = DeploymentPlacement.DevDuel(
			BattleUnitType.Fighter,
			BattleUnitType.Carrier,
			seed: 42,
			gridSize: 64);

		Assert.Equal(
			ShipCatalog.LoadoutForTier(BattleUnitType.Fighter, EShipGearTier.T0).InstalledAbilities,
			player.Ship.Loadout.InstalledAbilities);
		Assert.Equal(
			ShipCatalog.LoadoutForTier(BattleUnitType.Carrier, EShipGearTier.T0).InstalledAbilities,
			enemy.Ship.Loadout.InstalledAbilities);
		Assert.All(
			player.Ship.Loadout.InstalledAbilities.Concat(enemy.Ship.Loadout.InstalledAbilities),
			ability =>
			{
				Assert.Equal(0, ability.DamageUpgradeTier);
				Assert.Equal(0, ability.RangeUpgradeTier);
			});
	}

	[Fact]
	public void DevDuel_PlayerNotAtGridCenter()
	{
		var (player, _) = DeploymentPlacement.DevDuel(
			BattleUnitType.Fighter,
			BattleUnitType.Carrier,
			seed: 42,
			gridSize: 64);

		Assert.NotEqual(new Coord(32, 32, 32), player.Position);
	}

	[Fact]
	public void DevDuel_UnitsFaceEachOther()
	{
		var (player, enemy) = DeploymentPlacement.DevDuel(
			BattleUnitType.Fighter,
			BattleUnitType.Carrier,
			seed: 42,
			gridSize: 64);

		var delta = enemy.Position - player.Position;
		Assert.Equal(System.Math.Sign(delta.X), player.Fore.X);
		Assert.Equal(System.Math.Sign(-delta.X), enemy.Fore.X);
	}

	[Fact]
	public void DevDuel_EnemyPositionVariesBySeed()
	{
		var (_, enemyA) = DeploymentPlacement.DevDuel(
			BattleUnitType.Fighter, BattleUnitType.Carrier, seed: 1, gridSize: 64);
		var (_, enemyB) = DeploymentPlacement.DevDuel(
			BattleUnitType.Fighter, BattleUnitType.Carrier, seed: 2, gridSize: 64);

		Assert.NotEqual(enemyA.Position, enemyB.Position);
	}

	[Fact]
	public void DevDuel_KeepsEnemyInsideSmallGrid()
	{
		var (_, enemy) = DeploymentPlacement.DevDuel(
			BattleUnitType.Fighter, BattleUnitType.Carrier, seed: 8, gridSize: 16);

		Assert.InRange(enemy.Position.X, 0, 15);
		Assert.InRange(enemy.Position.Y, 0, 15);
		Assert.InRange(enemy.Position.Z, 0, 15);
	}

	[Fact]
	public void ForEngagement_VariedLayout_KeepsSixteenCellSeparation()
	{
		using var run = RunState.CreateNewRun(42);
		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		var enemyFleet = CreateEnemyFleet(run, "engagement-separation");

		for (var seed = 0; seed < 32; seed++)
		{
			var spawns = DeploymentPlacement.ForEngagement(
				[playerFleet, enemyFleet],
				run.ItemInventory,
				seed,
				gridSize: 64,
				playerFleet.State.Id);
			var player = RunTestKit.StarterFighterBattleSpawn(spawns);
			var enemy = spawns.Single(spawn => spawn.Team == ETeam.Enemy);

			Assert.InRange(
				BattleDistance.SeparationMeters(player.Position, enemy.Position),
				16_000f,
				20_000f);
		}
	}

	[Fact]
	public void ForEngagement_VariedLayout_ProducesDiagonalEngagementsAcrossSeeds()
	{
		using var run = RunState.CreateNewRun(42);
		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		var enemyFleet = CreateEnemyFleet(run, "engagement-angle");

		var deltas = new HashSet<Coord>();
		for (var seed = 0; seed < 48; seed++)
		{
			var spawns = DeploymentPlacement.ForEngagement(
				[playerFleet, enemyFleet],
				run.ItemInventory,
				seed,
				gridSize: 64,
				playerFleet.State.Id);
			var player = RunTestKit.StarterFighterBattleSpawn(spawns);
			var enemy = spawns.Single(spawn => spawn.Team == ETeam.Enemy);
			deltas.Add(enemy.Position - player.Position);
		}

		Assert.True(deltas.Count > 6);
		Assert.Contains(deltas, delta => delta.X != 0 && delta.Y != 0);
		Assert.Contains(deltas, delta => delta.X != 0 && delta.Z != 0);
	}

	[Fact]
	public void ForEngagement_CanonicalLayout_AlignsThroughGridCenter()
	{
		using var run = RunState.CreateNewRun(42);
		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		var enemyFleet = CreateEnemyFleet(run, "engagement-canonical");
		var center = new Coord(32, 32, 32);

		for (var seed = 0; seed < 16; seed++)
		{
			var spawns = DeploymentPlacement.ForEngagement(
				[playerFleet, enemyFleet],
				run.ItemInventory,
				seed,
				gridSize: 64,
				playerFleet.State.Id,
				canonicalLayout: true);
			var player = RunTestKit.StarterFighterBattleSpawn(spawns);
			var enemy = spawns.Single(spawn => spawn.Team == ETeam.Enemy);
			var delta = enemy.Position - player.Position;
			var axisComponents = (delta.X != 0 ? 1 : 0)
				+ (delta.Y != 0 ? 1 : 0)
				+ (delta.Z != 0 ? 1 : 0);

			Assert.Equal(1, axisComponents);
			Assert.Equal(
				center,
				new Coord(
					(player.Position.X + enemy.Position.X) / 2,
					(player.Position.Y + enemy.Position.Y) / 2,
					(player.Position.Z + enemy.Position.Z) / 2));
			Assert.Equal(16_000f, BattleDistance.SeparationMeters(player.Position, enemy.Position));
		}
	}

	[Fact]
	public void ForEngagement_TutorialScriptedLayout_PlacesEngagementOnXzPlane()
	{
		using var run = RunState.CreateNewRun(42);
		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		var enemyFleet = CreateEnemyFleet(run, "engagement-tutorial-scripted");
		var center = 32;

		for (var seed = 0; seed < 16; seed++)
		{
			var spawns = DeploymentPlacement.ForEngagement(
				[playerFleet, enemyFleet],
				run.ItemInventory,
				seed,
				gridSize: 64,
				playerFleet.State.Id,
				tutorialScriptedSpawn: true);
			var player = RunTestKit.StarterFighterBattleSpawn(spawns);
			var enemy = spawns.Single(spawn => spawn.Team == ETeam.Enemy);

			Assert.Equal(new Coord(center - 8, center, center), player.Position);
			Assert.Equal(new Coord(center + 8, center, center), enemy.Position);
			Assert.Equal(new Coord(1, 0, 0), player.Fore);
			Assert.Equal(Coord.Up, player.Dorsal);
			Assert.Equal(new Coord(-1, 0, 0), enemy.Fore);
			Assert.Equal(Coord.Up, enemy.Dorsal);
		}
	}

	[Fact]
	public void ForEngagement_TutorialScriptedLayout_SupportsTurnOneTutorialMove()
	{
		using var run = RunState.CreateNewRun(42);
		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		var enemyFleet = CreateEnemyFleet(run, "engagement-tutorial-turn1");

		for (var seed = 0; seed < 32; seed++)
		{
			var encounter = EngagementBattleFactory.Create(
				[playerFleet, enemyFleet],
				run.ItemInventory,
				seed,
				"tutorial-scripted",
				playerFleet.State.Id,
				tutorialScriptedSpawn: true);
			var player = RunTestKit.StarterFighterBattleSpawn(encounter.Spawns);
			var battle = BattleOrchestrator.FromEncounter(encounter, gridSize: 64);
			var basis = GridBasis.From(player.Fore, player.Dorsal, Coord.Cross(player.Dorsal, player.Fore));

			Assert.IsType<BattleTutorialObjectiveResult.Resolved>(
				BattleTutorialObjectives.ResolveTurn1(battle, basis));
		}
	}

	[Fact]
	public void DevDefault_UsesDeploymentPlacement()
	{
		var encounter = BattleEncounter.DevDefault(seed: 99, gridSize: 64);
		var player = encounter.Spawns.First(spawn => spawn.Team == ETeam.Player);
		var enemy = encounter.Spawns.First(spawn => spawn.Team == ETeam.Enemy);

		Assert.True(player.Position.X < enemy.Position.X);
		Assert.NotEqual(Coord.Forward, player.Fore);
	}

	[Fact]
	public void DevDefault_FieldsThreeClassesAtT0WithFullClassSkills()
	{
		var encounter = BattleEncounter.DevDefault(seed: 99, gridSize: 64);
		var expectedChassis = new[]
		{
			BattleUnitType.Fighter,
			BattleUnitType.RepurposedMiner,
			BattleUnitType.IndustrialGooper,
		};

		foreach (var team in new[] { ETeam.Player, ETeam.Enemy })
		{
			var teamSpawns = encounter.Spawns.Where(spawn => spawn.Team == team).ToArray();
			Assert.Equal(
				expectedChassis,
				teamSpawns.Select(spawn => spawn.Ship.Spec.Chassis).ToArray());

			foreach (var spawn in teamSpawns)
			{
				Assert.Equal(
					ShipCatalog.LoadoutForTier(spawn.Ship.Spec.Chassis, EShipGearTier.T0).InstalledAbilities,
					spawn.Ship.Loadout.InstalledAbilities);
				var expectedPilot = PilotBattleSnapshot.FromSkills(
					$"pilot-{spawn.Ship.Id}",
					PilotSkillCatalog.IdsForOperatorClass(spawn.Ship.Spec.ClassId));
				Assert.Equal(expectedPilot.PilotId, spawn.Pilot!.PilotId);
				Assert.Equal(expectedPilot.CapabilityGrants, spawn.Pilot.CapabilityGrants);
			}
		}
	}

	[Fact]
	public void DevDefault_PlacesClassmatesBesideAnchors()
	{
		for (var seed = 0; seed < 32; seed++)
		{
			var encounter = BattleEncounter.DevDefault(seed, gridSize: 64);
			var playerFighter = encounter.Spawns.Single(spawn =>
				spawn.Team == ETeam.Player && spawn.Ship.Spec.Chassis == BattleUnitType.Fighter);
			var playerGooper = encounter.Spawns.Single(spawn =>
				spawn.Team == ETeam.Player && spawn.Ship.Spec.Chassis == BattleUnitType.IndustrialGooper);
			var enemyFighter = encounter.Spawns.Single(spawn =>
				spawn.Team == ETeam.Enemy && spawn.Ship.Spec.Chassis == BattleUnitType.Fighter);
			var enemyGooper = encounter.Spawns.Single(spawn =>
				spawn.Team == ETeam.Enemy && spawn.Ship.Spec.Chassis == BattleUnitType.IndustrialGooper);

			Assert.Equal(4, playerFighter.Position.ManhattanDistanceTo(playerGooper.Position));
			Assert.Equal(4, enemyFighter.Position.ManhattanDistanceTo(enemyGooper.Position));
			Assert.Equal(playerFighter.Fore, playerGooper.Fore);
			Assert.Equal(enemyFighter.Fore, enemyGooper.Fore);
		}
	}

	[Fact]
	public void FromEncounter_AssignsGeneratedUnitIds()
	{
		var battle = GrimSpace.Battle.BattleOrchestrator.FromEncounter(
			BattleEncounter.DevDefault(seed: 7, gridSize: 32),
			gridSize: 32);

		Assert.Equal("fighter-dev-7", battle.PlayerId);
		Assert.Equal("fighter-enemy-dev-7", BattleTestFixture.FirstEnemyId(battle));
	}

	private static GrimSpace.World.StarSystem.Units.Fleet CreateEnemyFleet(RunState run, string fleetId)
	{
		var pirate = ContractEnemySpawner.CreateAmbushFleet(
			new FleetSpawnSpec(
				FleetType.PirateFleet,
				EFaction.Pirates,
				42,
				[(BattleUnitType.RepurposedMiner, EShipGearTier.T0)]),
			new Coord(20, 0, 20),
			fleetId,
			"deployment-placement-test");
		run.StarSystem.Map.FleetRegistry.Add(pirate);
		foreach (var declaration in pirate.Registrations)
			run.ItemInventory.Register(declaration, pirate.State.Id);
		return pirate;
	}

}
