using GrimSpace.Tests.Run;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Player;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem.Traffic;
using RunState = GrimSpace.Run.State;

namespace GrimSpace.Tests.World.StarSystem.Engagement;

[StarSystemTestSuite]
public sealed class BeginEngagementTests
{
	[Fact]
	public void BeginEngagement_SetsActiveBattleFromCommittedEngagement()
	{
		var run = RunState.CreateNewRun(42);
		var playerId = RunState.PlayerFleetUnitId;
		var pirateId = "pirate-a";
		var pirateFleet = StarSystemTestHarness.CreatePirateFleet(
			pirateId,
			new GrimSpace.Math.Grid.Coord(4, 0, 0),
			GrimSpace.World.Factions.EFaction.Pirates);
		run.StarSystem.Map.FleetRegistry.Add(pirateFleet);
		EnsureRegistry(run, pirateFleet);
		new SetEngagementIntentEffect(playerId, pirateId)
			.Apply(run.StarSystem.Map, new ActorRuntime(), playerId);
		new ReachContactEffect(playerId, pirateId)
			.Apply(run.StarSystem.Map, new ActorRuntime(), playerId);
		new CommitEngagementEffect(playerId, pirateId)
			.Apply(run.StarSystem.Map, new ActorRuntime(), playerId);

		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(playerId);
		pirateFleet = run.StarSystem.Map.FleetRegistry.FleetOf(pirateId);
		Assert.True(EngagementQueries.TryGetCommittedPlayerEngagement(run.StarSystem.Map, playerId, out var committed));
		var encounter = EngagementBattleFactory.Create(
			[playerFleet, pirateFleet],
			run.ItemInventory,
			7,
			committed.EngagementId,
			committed.InitiatorUnitId);
		Assert.Equal(playerId, committed.InitiatorUnitId);
		Assert.Equal(3, encounter.Spawns.Count);
		Assert.Equal(
			playerFleet.Members.Concat(pirateFleet.Members).Select(member => member.Id).Order(),
			encounter.Spawns.Select(spawn => spawn.Ship.Id).Order());
		Assert.All(
			encounter.Spawns,
			spawn => Assert.Equal(
				playerFleet.Members.Any(member => member.Id == spawn.Ship.Id)
					? ETeam.Player
					: ETeam.Enemy,
				spawn.Team));
		Assert.All(
			encounter.Spawns,
			spawn => Assert.Equal(
				OutcomeTestKit.ChassisFromShipId(spawn.Ship.Id),
				spawn.Ship.Spec.Chassis));
		Assert.All(
			encounter.Spawns.Where(spawn => spawn.Team == ETeam.Player),
			spawn => Assert.IsType<UserExecutionAgent>(spawn.ExecutionAgent));
		Assert.All(
			encounter.Spawns.Where(spawn => spawn.Team == ETeam.Enemy),
			spawn => Assert.IsType<AiController>(spawn.ExecutionAgent));
	}

	[Fact]
	public void Create_PlacesDuelOpponentsOutsideTurnOneWeaponReachWithAttackerFacingTarget()
	{
		var run = RunState.CreateNewRun(42);
		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		var pirateFleet = StarSystemTestHarness.CreatePirateFleet(
			"pirate-a",
			new Coord(4, 0, 0),
			GrimSpace.World.Factions.EFaction.Pirates);
		EnsureRegistry(run, pirateFleet);

		var encounter = EngagementBattleFactory.Create(
			[playerFleet, pirateFleet],
			run.ItemInventory,
			231,
			"test-engagement",
			playerFleet.State.Id,
			canonicalLayout: true);
		var player = RunTestKit.StarterFighterBattleSpawn(encounter.Spawns);
		var enemy = encounter.Spawns.Single(spawn => spawn.Team == ETeam.Enemy);

		Assert.Equal(16, player.Position.ManhattanDistanceTo(enemy.Position));
		Assert.Equal(player.Position + player.Fore * 16, enemy.Position);
	}

	[Fact]
	public void Create_UsesInitiatorAsAttacker()
	{
		var run = RunState.CreateNewRun(42);
		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		var pirateFleet = StarSystemTestHarness.CreatePirateFleet(
			"pirate-a",
			new Coord(4, 0, 0),
			GrimSpace.World.Factions.EFaction.Pirates);
		EnsureRegistry(run, pirateFleet);

		var encounter = EngagementBattleFactory.Create(
			[playerFleet, pirateFleet],
			run.ItemInventory,
			seed: 231,
			id: "test-engagement",
			initiatorFleetId: pirateFleet.State.Id,
			canonicalLayout: true);
		var player = RunTestKit.StarterFighterBattleSpawn(encounter.Spawns);
		var enemy = encounter.Spawns.Single(spawn => spawn.Team == ETeam.Enemy);

		Assert.Equal(enemy.Position + enemy.Fore * 16, player.Position);
	}

	[Fact]
	public void Create_RandomizesThreeDimensionalApproachAndInterceptedHeadingBySeed()
	{
		var run = RunState.CreateNewRun(42);
		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		var pirateFleet = StarSystemTestHarness.CreatePirateFleet(
			"pirate-a",
			new Coord(4, 0, 0),
			GrimSpace.World.Factions.EFaction.Pirates);
		EnsureRegistry(run, pirateFleet);
		var approachAxes = new HashSet<Coord>();
		var interceptedHeadings = new HashSet<(Coord Fore, Coord Dorsal)>();

		for (var seed = 0; seed < 24; seed++)
		{
			var encounter = EngagementBattleFactory.Create(
				[playerFleet, pirateFleet],
				run.ItemInventory,
				seed,
				$"test-engagement-{seed}",
				playerFleet.State.Id);
			var player = RunTestKit.StarterFighterBattleSpawn(encounter.Spawns);
			var enemy = encounter.Spawns.Single(spawn => spawn.Team == ETeam.Enemy);
			approachAxes.Add(new Coord(
				System.Math.Abs(player.Fore.X),
				System.Math.Abs(player.Fore.Y),
				System.Math.Abs(player.Fore.Z)));
			interceptedHeadings.Add((enemy.Fore, enemy.Dorsal));
		}

		Assert.Equal(3, approachAxes.Count);
		Assert.True(interceptedHeadings.Count > 1);
	}

	[Fact]
	public void Create_UsesUniqueRepurposedMinerPositions()
	{
		var run = RunState.CreateNewRun(42);
		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		var pirateFleet = StarSystemTestHarness.CreatePirateFleet(
			"pirate-a",
			new GrimSpace.Math.Grid.Coord(4, 0, 0),
			GrimSpace.World.Factions.EFaction.Pirates);
		pirateFleet = new Fleet(
			pirateFleet.State,
			Enumerable.Range(0, 5)
				.Select(index => new FleetMember($"repurposed-miner-{index}"))
				.ToArray());
		foreach (var member in pirateFleet.Members)
			run.ItemInventory.Register(
				ShipInstance.FromCatalog(member.Id, GrimSpace.Units.Enums.EType.RepurposedMiner),
				pirateFleet.State.Id);

		var encounter = EngagementBattleFactory.Create(
			[playerFleet, pirateFleet],
			run.ItemInventory,
			231,
			"test-engagement",
			playerFleet.State.Id);
		var positions = encounter.Spawns.Select(spawn => spawn.Position).ToArray();
		var intercepted = encounter.Spawns.Where(spawn => spawn.Team == ETeam.Enemy).ToArray();

		Assert.Equal(playerFleet.Members.Count + pirateFleet.Members.Count, positions.Length);
		Assert.Equal(positions.Length, positions.Distinct().Count());
		Assert.Single(intercepted.Select(spawn => spawn.Fore).Distinct());
		Assert.Single(intercepted.Select(spawn => spawn.Dorsal).Distinct());
	}

	[Fact]
	public void Create_AcceptsSignedGenerationSeed()
	{
		var run = RunState.CreateNewRun(42);
		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		var pirateFleet = StarSystemTestHarness.CreatePirateFleet(
			"pirate-a",
			new GrimSpace.Math.Grid.Coord(4, 0, 0),
			GrimSpace.World.Factions.EFaction.Pirates);
		EnsureRegistry(run, pirateFleet);

		var encounter = EngagementBattleFactory.Create(
			[playerFleet, pirateFleet],
			run.ItemInventory,
			-227155311,
			"test-engagement",
			playerFleet.State.Id);

		Assert.Equal(-227155311, encounter.Seed);
		Assert.NotEmpty(encounter.Spawns);
	}

	private static void EnsureRegistry(RunState run, Fleet fleet)
	{
		if (fleet.Registrations.Count > 0)
		{
			foreach (var declaration in fleet.Registrations)
				run.ItemInventory.Register(declaration, fleet.State.Id);
			return;
		}

		foreach (var member in fleet.Members)
			run.ItemInventory.Register(
				ShipInstance.FromCatalog(
					member.Id,
					OutcomeTestKit.ChassisFromShipId(member.Id)),
				fleet.State.Id);
	}
}
