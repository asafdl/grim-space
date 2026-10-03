using GrimSpace.Battle;
using GrimSpace.Battle.Encounter;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.Tutorials;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Specs;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Run.Persistence;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;

namespace GrimSpace.Tests.Run;

[IntegrationTestSuite]
public sealed class RunStatePersistenceTests
{
	[Fact]
	public void RegenerateMap_PreservesRunOwnedState()
	{
		using var run = State.CreateNewRun(42, tutorialsEnabled: true);
		var shipId = Assert.Single(run.PlayerParty.ShipIds);
		var ship = ShipInstance.FromSpec(
			shipId,
			FighterSpec.Instance,
			ShipCatalog.FullFighterLoadout());
		Assert.True(ship.TryWithUpgradedMaxHull(out ship));
		Assert.True(ship.TryWithUpgradedMaxShields(ESpatialOrientation.Forward, out ship));
		Assert.True(ship.TryWithDamageUpgraded(
			new AbilityMount(EAbilityKind.ScrapDroneSwarm, ESpatialOrientation.Port),
			out ship));
		Assert.True(ship.TryWithRangeUpgraded(
			new AbilityMount(EAbilityKind.ScrapDroneSwarm, ESpatialOrientation.Port),
			out ship));
		ship.HullPoints = 3;
		run.ShipRegistry.Update(ship);
		run.EnsurePlayerShipPortraits([shipId]);
		var portraitId = run.PlayerShipPortraitIds[shipId];
		run.TutorialState!.ActiveStepIndex = 2;
		run.TutorialState!.PendingTutorialGraduation = true;

		var previousMap = run.StarSystem.Map;
		run.RegenerateMap(99);

		Assert.NotSame(previousMap, run.StarSystem.Map);
		Assert.Equal(99, run.StarSystem.Map.Seed);
		Assert.Equal([shipId], run.PlayerParty.ShipIds);
		Assert.Same(ship, run.ShipRegistry.Get(shipId));
		Assert.Equal(3, run.ShipRegistry.Get(shipId).HullPoints);
		Assert.Equal(ship.Loadout.MaxHullPoints, run.ShipRegistry.Get(shipId).Loadout.MaxHullPoints);
		Assert.Equal(
			ship.Loadout.HullUpgradeTier,
			run.ShipRegistry.Get(shipId).Loadout.HullUpgradeTier);
		Assert.True(
			run.ShipRegistry.Get(shipId).Loadout.MaxShieldPoints.Matches(
				ship.Loadout.MaxShieldPoints));
		Assert.True(
			run.ShipRegistry.Get(shipId).Loadout.ShieldUpgradeTiers.Matches(
				ship.Loadout.ShieldUpgradeTiers));
		Assert.Equal(
			ship.Loadout.InstalledAbilities,
			run.ShipRegistry.Get(shipId).Loadout.InstalledAbilities);
		Assert.Equal(portraitId, run.PlayerShipPortraitIds[shipId]);
		Assert.NotNull(run.Tutorials);
		Assert.Equal(2, run.TutorialState!.ActiveStepIndex);
		Assert.True(run.TutorialState.PendingTutorialGraduation);
		Assert.Contains(
			run.StarSystem.Map.FleetRegistry.FleetOf(State.PlayerFleetUnitId).Members,
			member => member.Id == shipId);
	}

	[Fact]
	public void ActiveBattle_PreservesEncounterIdentityInRunState()
	{
		using var run = State.CreateNewRun(42);
		var encounter = BattleEncounter.DevDefault(123);

		run.ActiveBattle = encounter;

		Assert.Same(encounter, run.ActiveBattle);
		Assert.StartsWith("engagement-", encounter.Id);
		Assert.Equal(123, run.ActiveBattle.Seed);
		Assert.Contains(
			run.ActiveBattle.Spawns,
			spawn => spawn.Ship.Spec.Chassis == EType.Fighter);
	}

	[Fact]
	public void Snapshot_RestoresRunOwnedStateAndBindings()
	{
		using var run = State.CreateNewRun(42, tutorialsEnabled: true);
		var shipId = Assert.Single(run.PlayerParty.ShipIds);
		run.EnsurePlayerShipPortraits([shipId]);
		run.TutorialState!.ActiveStepIndex = 2;
		run.TutorialState!.PendingTutorialGraduation = true;
		run.ActiveBattle = BattleEncounter.DevDefault(123, 12);
		run.StarSystem.SetStepped();
		var registry = PersistenceRegistry.CreateDefault();

		var snapshot = run.CaptureSnapshot(registry);
		using var restored = State.FromSnapshot(snapshot, registry);

		Assert.Equal(run.PlayerParty.ShipIds, restored.PlayerParty.ShipIds);
		Assert.Equal(run.ShipRegistry.Get(shipId).HullPoints, restored.ShipRegistry.Get(shipId).HullPoints);
		Assert.Equal(run.PlayerShipPortraitIds[shipId], restored.PlayerShipPortraitIds[shipId]);
		Assert.Equal(2, restored.TutorialState!.ActiveStepIndex);
		Assert.True(restored.Tutorials is not null);
		Assert.Equal(run.ActiveBattle!.Id, restored.ActiveBattle!.Id);
		Assert.Equal(ESimMode.Stepped, restored.StarSystem.SimMode);

		var restoredHunt = Assert.IsType<HuntObjective>(
			restored.StarSystem.Map.ContractRegistry.All
				.Single(contract => contract.IsStoryObjective)
				.Objective);
		Assert.All(
			restoredHunt.SpawnGroups[0].Spawn.Members,
			member => Assert.NotEqual(EType.Fighter, member.Chassis));
	}

	[Fact]
	public void Snapshot_RestoredTutorialHuntBuildsNonFighterBattleShips()
	{
		using var run = State.CreateNewRun(42, tutorialsEnabled: true);
		var registry = PersistenceRegistry.CreateDefault();
		var snapshot = run.CaptureSnapshot(registry);
		using var restored = State.FromSnapshot(snapshot, registry);

		var contract = restored.StarSystem.Map.ContractRegistry.All
			.Single(candidate => candidate.IsStoryObjective);
		var spawned = ContractEnemySpawner.SpawnForContract(
			contract,
			restored.StarSystem.Map,
			"test-member");
		var enemyFleet = Assert.Single(spawned.Fleets);
		foreach (var declaration in enemyFleet.Registrations)
			restored.ShipRegistry.Register(declaration);

		var playerFleet = restored.StarSystem.Map.FleetRegistry.FleetOf(
			State.PlayerFleetUnitId);
		var encounter = EngagementBattleFactory.Create(
			[playerFleet, enemyFleet],
			restored.ShipRegistry,
			seed: 42,
			id: "restored-tutorial-hunt");

		Assert.All(
			encounter.Spawns.Where(spawn => spawn.Team == ETeam.Enemy),
			spawn => Assert.NotEqual(EType.Fighter, spawn.Ship.Spec.Chassis));
	}

}
