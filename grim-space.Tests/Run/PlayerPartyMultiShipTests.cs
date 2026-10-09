using GrimSpace.Battle;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Player;
using GrimSpace.Core.Ids;
using GrimSpace.Math.Grid;
using GrimSpace.Run.Persistence;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.FleetSpawner;
using GrimSpace.World.StarSystem.Poi.Concrete;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem.Merchants;
using GrimSpace.Tests.World.StarSystem.Poi;
using FleetType = GrimSpace.World.StarSystem.Units.EType;
using BattleUnitType = GrimSpace.Units.Enums.EType;
using RunState = GrimSpace.Run.State;

namespace GrimSpace.Tests.Run;

[IntegrationTestSuite]
public sealed class PlayerPartyMultiShipTests
{
	[Fact]
	public void Snapshot_RestoresSelectedFleetMember()
	{
		using var run = RunState.CreateNewRun(42);
		var secondId = TypedIdGenerator.NextId("gunship");
		Assert.True(run.TryEnlistPlayerShip(new ShipSpawnDeclaration(secondId, BattleUnitType.Gunship)));
		Assert.True(run.StarSystem.TryCommitPlayerInput(
			new SelectActiveFleetShipAction(RunState.PlayerFleetUnitId, secondId)));

		var registry = PersistenceRegistry.CreateDefault();
		var snapshot = run.CaptureSnapshot(registry);
		using var restored = RunState.FromSnapshot(snapshot, registry);

		Assert.Equal(
			secondId,
			restored.StarSystem.RuntimeFor(RunState.PlayerFleetUnitId).SelectedMemberShipId);
	}

	[Fact]
	public void CreateActiveBattleOrchestrator_PrefersSelectedPlayerWhenBattleWorldIsRestoredOver()
	{
		using var run = RunState.CreateNewRun(42);
		var first = run.PlayerParty.ShipIds[0];
		var secondId = TypedIdGenerator.NextId("gunship");
		Assert.True(run.TryEnlistPlayerShip(new ShipSpawnDeclaration(secondId, BattleUnitType.Gunship)));
		Assert.True(run.StarSystem.TryCommitPlayerInput(
			new SelectActiveFleetShipAction(RunState.PlayerFleetUnitId, secondId)));

		var encounter = new BattleEncounter
		{
			Id = "multi-ship-restore",
			Seed = 11,
			Objective = EObjective.EliminateOpponents,
			Spawns =
			[
				BattleSpawnTestKit.Create(
					run.ShipRegistry.Get(first).Clone(),
					ETeam.Player,
					new Coord(3, 1, 1),
					new UserExecutionAgent()),
				BattleSpawnTestKit.Create(
					run.ShipRegistry.Get(secondId).Clone(),
					ETeam.Player,
					new Coord(5, 1, 1),
					new UserExecutionAgent()),
				BattleSpawnTestKit.Create(
					"enemy",
					BattleUnitType.Fighter,
					ETeam.Enemy,
					new Coord(8, 1, 1),
					new AiController()),
			],
		};
		using var battle = BattleOrchestrator.FromEncounter(encounter, gridSize: 12);
		battle.Engine.World.battleResult = EBattleResult.Win;

		run.ActiveBattle = encounter;
		var registry = PersistenceRegistry.CreateDefault();
		var snapshot = run.CaptureSnapshot(registry, battle.Engine.World);
		using var restoredRun = RunState.FromSnapshot(snapshot, registry);

		using var orchestrator = restoredRun.CreateActiveBattleOrchestrator();

		Assert.Equal(secondId, orchestrator.ActivePlayerId);
		Assert.NotEqual(first, orchestrator.ActivePlayerId);
	}

	[Fact]
	public void CreateNewRun_MerchantPurchaseAppliesToSelectedShip()
	{
		using var run = RunState.CreateNewRun(42);
		var first = run.PlayerParty.ShipIds[0];
		var secondId = TypedIdGenerator.NextId("gunship");
		Assert.True(run.TryEnlistPlayerShip(new ShipSpawnDeclaration(secondId, BattleUnitType.Gunship)));
		Assert.True(run.StarSystem.TryCommitPlayerInput(
			new SelectActiveFleetShipAction(RunState.PlayerFleetUnitId, secondId)));

		var selected = run.ShipRegistry.Get(secondId);
		selected.HullPoints = 1;
		run.ShipRegistry.Update(selected);
		var offering = MerchantPurchaseTestHarness.RepairHull;
		SeedResources(run.StarSystem.Map, credits: 500, scrap: 500);
		var before = run.ShipRegistry.Get(secondId).Clone();
		var action = new PurchaseAction(
			RunState.PlayerFleetUnitId,
			SupplySystemPlan.Copper.TradeHubPoiId,
			"poi-trade-dockyard",
			MapFacilityOperators.ShieldOperatorName(run.StarSystem.Map),
			EMerchantCatalog.ShipSupport,
			offering,
			before);
		var runtime = run.StarSystem.RuntimeFor(RunState.PlayerFleetUnitId);
		Assert.True(PurchaseActionDef.Instance.IsLegal(action, run.StarSystem.Map, runtime));

		run.StarSystem.CommitSetup(action);

		var repaired = run.ShipRegistry.Get(secondId);
		var untouched = run.ShipRegistry.Get(first);
		Assert.Equal(repaired.Loadout.MaxHullPoints, repaired.HullPoints);
		Assert.Equal(untouched.Loadout.MaxHullPoints, untouched.HullPoints);
	}

	[Fact]
	public void EngagementFactory_SpawnsEveryPartyShipInPlayerFleet()
	{
		using var run = RunState.CreateNewRun(42);
		var secondId = TypedIdGenerator.NextId("gunship");
		Assert.True(run.TryEnlistPlayerShip(new ShipSpawnDeclaration(secondId, BattleUnitType.Gunship)));

		var enemyFleet = CreateEnemyFleet(run);
		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		var encounter = EngagementBattleFactory.Create(
			[playerFleet, enemyFleet],
			run.ShipRegistry,
			seed: 3,
			id: "multi-ship-engagement");

		Assert.Equal(
			run.PlayerParty.ShipIds.OrderBy(id => id, StringComparer.Ordinal),
			encounter.Spawns
				.Where(spawn => spawn.Team == ETeam.Player)
				.Select(spawn => spawn.Ship.Id)
				.OrderBy(id => id, StringComparer.Ordinal));
	}

	private static Fleet CreateEnemyFleet(RunState run)
	{
		var map = run.StarSystem.Map;
		var pirate = ContractEnemySpawner.CreateAmbushFleet(
			new FleetSpawnSpec(
				FleetType.PirateFleet,
				EFaction.Pirates,
				42,
				[(BattleUnitType.RepurposedMiner, EShipGearTier.T0)]),
			new Coord(20, 0, 20),
			"multi-ship-pirate",
			"test");
		map.FleetRegistry.Add(pirate);
		foreach (var declaration in pirate.Registrations)
			run.ShipRegistry.Register(declaration);
		return pirate;
	}

	private static void SeedResources(StarMap map, int credits, int scrap)
	{
		var runtime = new ActorRuntime();
		new ChangeResourceEffect(
				TransactionSource.MerchantPurchase,
				ResourceBundle.Create(
					(ResourceId.Credits, credits),
					(ResourceId.ScrapAlloy, scrap)))
			.Apply(map, runtime, "seed");
	}
}
