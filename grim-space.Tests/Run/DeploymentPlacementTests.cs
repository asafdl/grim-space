using GrimSpace.Tests.Run;
using GrimSpace.Battle;
using GrimSpace.Battle.Presentation;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Movement;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Encounter.Generation;
using GrimSpace.Battle.PilotCapabilities;
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
	public void DevDefault_IncludesRogueAsFourthPlayerShipWithoutChangingEnemies()
	{
		var encounter = BattleEncounter.DevDefault();
		var players = encounter.Spawns.Where(spawn => spawn.Team == ETeam.Player).ToArray();
		var enemies = encounter.Spawns.Where(spawn => spawn.Team == ETeam.Enemy).ToArray();

		Assert.Equal(
			new[] { EType.Fighter, EType.RepurposedMiner, EType.IndustrialGooper, EType.Rogue },
			players.Select(spawn => spawn.Ship.Spec.Chassis));
		Assert.Equal(
			new[] { EType.Fighter, EType.Carrier, EType.IndustrialGooper },
			enemies.Select(spawn => spawn.Ship.Spec.Chassis));
		Assert.Equal(encounter.Spawns.Count, encounter.Spawns.Select(spawn => spawn.Position).Distinct().Count());
		var rogue = players[3];
		Assert.NotNull(rogue.Pilot);
		Assert.IsType<GrimSpace.Battle.Player.UserExecutionAgent>(rogue.ExecutionAgent);
		Assert.Equal(3, rogue.Ship.Loadout.InstalledAbilities.Count);
		Assert.Equal(players[0].Position + players[0].Dorsal * 4, rogue.Position);
	}

	[Fact]
	public void ForEngagement_VariedLayout_KeepsTenToTwelveCellSeparation()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		var enemyFleet = CreateEnemyFleet(run, "engagement-separation");

		for (var seed = 0; seed < 32; seed++)
		{
			var spawns = DeploymentPlacement.ForEngagement(
				[playerFleet, enemyFleet],
				run.ItemInventory,
				seed,
				gridSize: 20,
				playerFleet.State.Id);
			var player = RunTestKit.StarterFighterBattleSpawn(spawns);
			var enemy = spawns.Single(spawn => spawn.Team == ETeam.Enemy);

			Assert.InRange(
				BattleDistance.SeparationMeters(player.Position, enemy.Position),
				10_000f,
				12_000f);
		}
	}

	[Fact]
	public void ForEngagement_VariedLayout_SpreadsGravityWellsAcrossElevations()
	{
		using var run = RunState.CreateNewRun(seed: 0, tutorialsEnabled: true);
		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		var enemyFleet = CreateEnemyFleet(run, "engagement-gravity-wells");
		for (var seed = 0; seed < 128; seed++)
		{
			var spawns = DeploymentPlacement.ForEngagement(
				[playerFleet, enemyFleet], run.ItemInventory, seed, gridSize: 20, playerFleet.State.Id);
			var context = new HazardPlacementContext { Rng = new Random(seed), GridSize = 20, Spawns = spawns };
			new GravityWellPlacement().Place(context);

			Assert.Equal(4, context.Placed.Count);
			var playerCenter = SingleCellHazardPlacement.Centroid(
				spawns.Where(spawn => spawn.Team == ETeam.Player).Select(spawn => spawn.Position).ToArray());
			var enemyCenter = SingleCellHazardPlacement.Centroid(
				spawns.Where(spawn => spawn.Team == ETeam.Enemy).Select(spawn => spawn.Position).ToArray());
			var midpointHeight = (playerCenter.Y + enemyCenter.Y) / 2;
			Assert.Contains(context.Placed, well => well.Origin.Y >= midpointHeight + 3);
			Assert.Contains(context.Placed, well => well.Origin.Y <= midpointHeight - 3);
			for (var i = 0; i < context.Placed.Count; i++)
			for (var j = i + 1; j < context.Placed.Count; j++)
			{
				var delta = context.Placed[i].Origin - context.Placed[j].Origin;
				Assert.True(Coord.Dot(delta, delta) >= 36);
			}
		}
	}

	[Fact]
	public void ForEngagement_GeneratesCompactHazards()
	{
		using var run = RunState.CreateNewRun(seed: 0, tutorialsEnabled: true);
		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		var enemyFleet = CreateEnemyFleet(run, "engagement-gravity-hazards");
		for (var seed = 0; seed < 8; seed++)
		{
			var encounter = EngagementBattleFactory.Create(
				[playerFleet, enemyFleet], run.ItemInventory, seed, "gravity-test", playerFleet.State.Id);
			Assert.Equal(4, encounter.WorldHazards.Count(h => h.Kind == EBattleHazardKind.GravityWell));
			Assert.Equal(10, encounter.WorldHazards.Count(h => h.Kind == EBattleHazardKind.CryoPlume));
			Assert.Equal(10, encounter.WorldHazards.Count(h => h.Kind == EBattleHazardKind.IonStorm));
			Assert.Equal(30, encounter.WorldHazards.Count(h => h.Kind == EBattleHazardKind.AsteroidBelt));
		}
	}

	[Fact]
	public void ForEngagement_VariedLayout_ProducesDiagonalEngagementsAcrossSeeds()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		var enemyFleet = CreateEnemyFleet(run, "engagement-angle");

		var deltas = new HashSet<Coord>();
		for (var seed = 0; seed < 48; seed++)
		{
			var spawns = DeploymentPlacement.ForEngagement(
				[playerFleet, enemyFleet],
				run.ItemInventory,
				seed,
				gridSize: 20,
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
		using var run = RunTestKit.CreateFighterSupportRun();
		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		var enemyFleet = CreateEnemyFleet(run, "engagement-canonical");
		var center = new Coord(10, 10, 10);

		for (var seed = 0; seed < 16; seed++)
		{
			var spawns = DeploymentPlacement.ForEngagement(
				[playerFleet, enemyFleet],
				run.ItemInventory,
				seed,
				gridSize: 20,
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
			Assert.Equal(12_000f, BattleDistance.SeparationMeters(player.Position, enemy.Position));
		}
	}

	[Fact]
	public void ForEngagement_TutorialScriptedLayout_PlacesLeadFighterBelowTargetAndBackupFar()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		var enemyFleet = CreateEnemyFleet(run, "engagement-tutorial-scripted", minerCount: 3);
		var target = new Coord(8, 10, 10);

		var spawns = DeploymentPlacement.ForEngagement(
			[playerFleet, enemyFleet],
			run.ItemInventory,
			seed: 7,
			gridSize: 20,
			enemyFleet.State.Id,
			tutorialScriptedSpawn: true);
		var fighter = RunTestKit.StarterFighterBattleSpawn(spawns);
		var enemies = spawns.Where(spawn => spawn.Team == ETeam.Enemy).ToArray();

		Assert.Equal(target + new Coord(-3, -3, 0), fighter.Position);
		Assert.Equal(new Coord(1, 0, 0), fighter.Fore);
		Assert.Equal(
			[target, target + new Coord(11, 1, -6), target + new Coord(11, 1, 6)],
			enemies.Select(spawn => spawn.Position));
		Assert.All(enemies, enemy => Assert.Equal(new Coord(-1, 0, 0), enemy.Fore));
		Assert.Equal(spawns.Count, spawns.Select(spawn => spawn.Position).Distinct().Count());
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(7)]
	public void ForEngagement_TutorialScriptedLayout_HeadOnShotIsAbsorbedAndAKillShotIsReachable(int seed)
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		var enemyFleet = CreateEnemyFleet(run, "engagement-tutorial-shots", minerCount: 3);
		var target = new Coord(8, 10, 10);
		var encounter = EngagementBattleFactory.Create(
			[playerFleet, enemyFleet],
			run.ItemInventory,
			seed: seed,
			"tutorial-scripted",
			playerFleet.State.Id,
			tutorialScriptedSpawn: true);
		var targetId = encounter.Spawns.Single(spawn => spawn.Position == target).Ship.Id;
		Assert.Empty(encounter.WorldHazards);
		using var battle = BattleOrchestrator.FromEncounter(encounter);
		Assert.Equal(20, battle.Engine.World.Grid.Width);
		Assert.All(encounter.Spawns, spawn => Assert.True(battle.Engine.World.Grid.IsInBounds(spawn.Position)));
		Assert.IsType<BattleTutorialObjectiveResult.Resolved>(BattleTutorialObjectives.ResolveShieldApproach(battle));
		var sim = battle.PlayerAgent.Sim;
		var paths = MovePathEndpoints.DiscoverExtensions(sim, battle.PlayerId);

		var headOn = paths.First(path =>
			path.EndPosition == target + new Coord(-3, 0, 0) && path.EndBasis.Forward == new Coord(1, 0, 0));
		var headOnImpact = Assert.Single(CannonImpactsOn(sim, battle.PlayerId, targetId, headOn.Steps));
		Assert.True(headOnImpact.ShieldDamage > 0);
		Assert.Equal(0, headOnImpact.HullDamage);

		Assert.Contains(paths, path =>
			CannonImpactsOn(sim, battle.PlayerId, targetId, path.Steps).Any(impact => impact.IsLethal));
	}

	[Fact]
	public void EngagementBattle_UsesCompactGridAndHazardCounts()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		var enemyFleet = CreateEnemyFleet(run, "compact-engagement", minerCount: 3);
		var approachDirections = new HashSet<Coord>();

		for (var seed = 0; seed < 32; seed++)
		{
			var encounter = EngagementBattleFactory.Create(
				[playerFleet, enemyFleet], run.ItemInventory, seed, $"compact-{seed}", playerFleet.State.Id);
			using var battle = BattleOrchestrator.FromEncounter(encounter);
			var grid = battle.Engine.World.Grid;
			Assert.Equal((20, 20, 20), (grid.Width, grid.Height, grid.Depth));
			Assert.All(encounter.Spawns, spawn => Assert.True(grid.IsInBounds(spawn.Position)));
			Assert.All(encounter.WorldHazards, hazard =>
				Assert.All(hazard.Cells, cell => Assert.True(grid.IsInBounds(cell))));
			Assert.Equal(4, encounter.WorldHazards.Count(h => h.Kind == EBattleHazardKind.GravityWell));
			Assert.Equal(10, encounter.WorldHazards.Count(h => h.Kind == EBattleHazardKind.CryoPlume));
			Assert.Equal(10, encounter.WorldHazards.Count(h => h.Kind == EBattleHazardKind.IonStorm));
			Assert.Equal(30, encounter.WorldHazards.Count(h => h.Kind == EBattleHazardKind.AsteroidBelt));
			var player = encounter.Spawns.First(spawn => spawn.Team == ETeam.Player);
			var enemy = encounter.Spawns.First(spawn => spawn.Team == ETeam.Enemy);
			approachDirections.Add(enemy.Position - player.Position);
		}
		Assert.Contains(approachDirections, delta =>
			(delta.X != 0 ? 1 : 0) + (delta.Y != 0 ? 1 : 0) + (delta.Z != 0 ? 1 : 0) > 1);
	}

	private static IEnumerable<ImpactFacts> CannonImpactsOn(
		BattleSimulation sim,
		string playerId,
		string targetId,
		IReadOnlyList<IAction> steps)
	{
		var fork = sim.ForkFromAnchor();
		if (!steps.All(step => fork.TryEnqueue(step)))
			return [];

		return fork.Peek(new LightningCannonAction(playerId))?.Records
			.OfType<Record<ImpactFacts>>()
			.Select(record => record.Value)
			.Where(impact => impact.TargetId == targetId)
			?? [];
	}

	[Fact]
	public void DevDefault_PlacesOpposingFormations()
	{
		var encounter = BattleEncounter.DevDefault(seed: 99);
		var player = encounter.Spawns.First(spawn => spawn.Team == ETeam.Player);
		var enemy = encounter.Spawns.First(spawn => spawn.Team == ETeam.Enemy);

		Assert.True(player.Position.X < enemy.Position.X);
		Assert.NotEqual(Coord.Forward, player.Fore);
	}

	[Fact]
	public void DevDefault_FieldsFourClassesAtT0WithFullClassSkills()
	{
		var encounter = BattleEncounter.DevDefault(seed: 99);
		Assert.Equal(
			[
				BattleUnitType.Fighter,
				BattleUnitType.RepurposedMiner,
				BattleUnitType.IndustrialGooper,
				BattleUnitType.Rogue,
			],
			encounter.Spawns.Where(spawn => spawn.Team == ETeam.Player)
				.Select(spawn => spawn.Ship.Spec.Chassis)
				.ToArray());
		Assert.Equal(
			[
				BattleUnitType.Fighter,
				BattleUnitType.Carrier,
				BattleUnitType.IndustrialGooper,
			],
			encounter.Spawns.Where(spawn => spawn.Team == ETeam.Enemy)
				.Select(spawn => spawn.Ship.Spec.Chassis)
				.ToArray());

		foreach (var spawn in encounter.Spawns)
		{
			Assert.Equal(
				ShipCatalog.LoadoutForTier(spawn.Ship.Spec.Chassis, EShipGearTier.T0).InstalledAbilities,
				spawn.Ship.Loadout.InstalledAbilities);
			if (spawn.Ship.Spec.ClassId == EOperatorClassId.Unclassified)
			{
				Assert.Null(spawn.Pilot);
				continue;
			}

			var expectedPilot = PilotBattleSnapshot.FromSkills(
				spawn.Pilot!.PilotId,
				PilotSkillCatalog.IdsForOperatorClass(spawn.Ship.Spec.ClassId));
			Assert.Equal(expectedPilot.CapabilityGrants, spawn.Pilot.CapabilityGrants
				.Where(grant => grant.Source.Kind == EPilotCapabilitySourceKind.Skill));
			if (spawn.Team == ETeam.Player)
			{
				var pilot = Assert.Single(encounter.DevPilots, pilot => pilot.Id == spawn.PilotId);
				Assert.Equal(spawn.Ship.Id, pilot.AssignedShipId);
				Assert.Equal(spawn.Ship.Spec.ClassId, pilot.ClassId);
				Assert.False(string.IsNullOrWhiteSpace(pilot.Name));
				Assert.Equal(pilot.PortraitId, spawn.Pilot.PortraitId);
				Assert.Empty(pilot.PositiveQuirkIds);
				Assert.Empty(pilot.NegativeQuirkIds);
				Assert.Equal(
					PilotCapabilityGrant.FromBackground(
						pilot.Id,
						pilot.BackgroundId,
						BattlePilotCapabilityPipelineFactory.Supports),
					spawn.Pilot.CapabilityGrants
						.Where(grant => grant.Source.Kind == EPilotCapabilitySourceKind.Background));
			}
			else
			{
				Assert.Equal($"pilot-{spawn.Ship.Id}", spawn.PilotId);
				Assert.Equal(expectedPilot.CapabilityGrants, spawn.Pilot.CapabilityGrants);
			}
		}
	}

	[Fact]
	public void DevDefault_PlacesClassmatesBesideAnchors()
	{
		for (var seed = 0; seed < 32; seed++)
		{
			var encounter = BattleEncounter.DevDefault(seed);
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
			BattleEncounter.DevDefault(seed: 7));

		Assert.Equal("rogue-dev-7", battle.PlayerId);
		Assert.Equal("fighter-enemy-dev-7", BattleTestFixture.FirstEnemyId(battle));
	}

	private static GrimSpace.World.StarSystem.Units.Fleet CreateEnemyFleet(
		RunState run,
		string fleetId,
		int minerCount = 1)
	{
		var pirate = ContractEnemySpawner.CreateAmbushFleet(
			new FleetSpawnSpec(
				FleetType.PirateFleet,
				EFaction.Pirates,
				42,
				Enumerable.Repeat((BattleUnitType.RepurposedMiner, EShipGearTier.T0), minerCount).ToArray()),
			new Coord(20, 0, 20),
			fleetId,
			"deployment-placement-test");
		run.StarSystem.Map.FleetRegistry.Add(pirate);
		foreach (var declaration in pirate.Registrations)
			run.ItemInventory.Register(declaration, pirate.State.Id);
		return pirate;
	}

}
