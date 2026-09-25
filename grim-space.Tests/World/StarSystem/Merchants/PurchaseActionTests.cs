using GrimSpace.Battle.Encounter;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi.Concrete;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.Tests.World.StarSystem;
using GrimSpace.Tests.World.StarSystem.Poi;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

[StarSystemTestSuite]
public sealed class PurchaseActionTests(StarMapFixture maps)
{
	private const string DockyardFacilityId = "poi-trade-dockyard";

	[Fact]
	public void Commit_WeaponsUpgrade_DebitsScrapAndEmitsPurchaseRecord()
	{
		var (engine, unitId, ship, _) = MerchantPurchaseTestHarness.CreateEngine(maps);
		SeedScrap(engine.World, 100);
		var before = ship.Clone();
		var offering = MerchantPurchaseTestHarness.FlakPortDamageUpgrade;
		var initialScrap = engine.World.PlayerResources.GetBalance(ResourceId.ScrapAlloy);

		var records = engine.Commit(CreateAction(
			unitId,
			engine.World,
			EMerchantCatalog.Weapons,
			offering,
			before));

		Assert.Contains(records, record => record is Record<MerchantShipPurchase>);
		Assert.True(MerchantCatalog.TryFind(EMerchantCatalog.Weapons, offering, ship, out var offer));
		Assert.True(offer.Cost.TryGet(ResourceId.ScrapAlloy, out var scrapCost));
		Assert.Equal(initialScrap - scrapCost, engine.World.PlayerResources.GetBalance(ResourceId.ScrapAlloy));
	}

	[Fact]
	public void TryEnqueue_FailsWhenScrapInsufficient()
	{
		var (engine, unitId, ship, _) = MerchantPurchaseTestHarness.CreateEngine(maps);
		var sim = engine.CreateSimulation();
		var before = ship.Clone();

		Assert.False(sim.TryEnqueue(CreateAction(
			unitId,
			engine.World,
			EMerchantCatalog.Weapons,
			MerchantPurchaseTestHarness.FlakPortDamageUpgrade,
			before)));
	}

	[Fact]
	public void TryEnqueue_FailsForStaleBeforeSnapshot()
	{
		var (engine, unitId, ship, _) = MerchantPurchaseTestHarness.CreateEngine(maps);
		SeedScrap(engine.World, 100);
		var sim = engine.CreateSimulation();
		var before = ship.Clone();
		var mount = new AbilityMount(EAbilityKind.Flak, ESpatialOrientation.Port);
		var stale = before.Clone();
		for (var tier = 0; tier < FlakSpec.MaxDamageUpgradeTier; tier++)
			Assert.True(stale.TryWithDamageUpgraded(mount, out stale));

		Assert.False(sim.TryEnqueue(CreateAction(
			unitId,
			engine.World,
			EMerchantCatalog.Weapons,
			MerchantPurchaseTestHarness.FlakPortDamageUpgrade,
			stale)));
	}

	[Fact]
	public void TryEnqueue_FailsWhenRegistryMutatedAfterBeforeSnapshot()
	{
		var (engine, unitId, ship, registry) = MerchantPurchaseTestHarness.CreateEngine(maps);
		SeedScrap(engine.World, 100);
		var sim = engine.CreateSimulation();
		var before = ship.Clone();
		Assert.True(ship.TryWithDamageUpgraded(
			new AbilityMount(EAbilityKind.Flak, ESpatialOrientation.Port),
			out ship));
		registry.Update(ship);

		Assert.False(sim.TryEnqueue(CreateAction(
			unitId,
			engine.World,
			EMerchantCatalog.Weapons,
			MerchantPurchaseTestHarness.FlakPortDamageUpgrade,
			before)));
	}

	[Fact]
	public void CreateNewRun_AppliesUpgradeToRegistryWhenPurchaseCommits()
	{
		using var run = State.CreateNewRun(42);
		var shipId = run.PlayerParty.ShipIds[0];
		var ship = run.ShipRegistry.Get(shipId);
		var offering = MerchantPurchaseTestHarness.FlakPortDamageUpgrade;
		SeedScrap(run.StarSystem.Map, 200);
		var before = ship.Clone();
		var action = CreateAction(
			State.PlayerFleetUnitId,
			run.StarSystem.Map,
			EMerchantCatalog.Weapons,
			offering,
			before);
		var runtime = run.StarSystem.RuntimeFor(State.PlayerFleetUnitId);
		Assert.True(PurchaseActionDef.Instance.IsLegal(action, run.StarSystem.Map, runtime));

		MerchantShipPurchase? purchase = null;
		using var subscription = run.StarSystem.Subscribe<Record<MerchantShipPurchase>>(record =>
			purchase = record.Value);

		run.StarSystem.CommitSetup(action);
		Assert.NotNull(purchase);

		var updated = run.ShipRegistry.Get(shipId);
		Assert.NotEqual(before.Spec, updated.Spec);
		Assert.True(MerchantCatalog.TryFind(EMerchantCatalog.Weapons, offering, updated, out var nextOffer));
		Assert.True(nextOffer.Cost.TryGet(ResourceId.ScrapAlloy, out var nextScrap));
		Assert.Equal(60, nextScrap);
	}

	[Fact]
	public void TryEnqueue_FailsWhenWeaponsMerchantOperatorForged()
	{
		var (engine, unitId, ship, _) = MerchantPurchaseTestHarness.CreateEngine(maps);
		SeedScrap(engine.World, 100);
		var sim = engine.CreateSimulation();
		var before = ship.Clone();

		Assert.False(sim.TryEnqueue(new PurchaseAction(
			unitId,
			SupplySystemPlan.Copper.TradeHubPoiId,
			DockyardFacilityId,
			MapFacilityOperators.ShieldOperatorName(engine.World),
			EMerchantCatalog.Weapons,
			MerchantPurchaseTestHarness.FlakPortDamageUpgrade,
			before)));
	}

	[Fact]
	public void Commit_HullRepair_DebitsResourcesAndEmitsPurchaseRecord()
	{
		var (engine, unitId, ship, _) = MerchantPurchaseTestHarness.CreateEngine(maps);
		ship.HullPoints = 1;
		SeedResources(engine.World, credits: 100, scrap: 100);
		var before = ship.Clone();
		var offering = MerchantPurchaseTestHarness.RepairHull;
		var initialCredits = engine.World.PlayerResources.GetBalance(ResourceId.Credits);
		var initialScrap = engine.World.PlayerResources.GetBalance(ResourceId.ScrapAlloy);

		var records = engine.Commit(CreateAction(
			unitId,
			engine.World,
			EMerchantCatalog.ShipSupport,
			offering,
			before));

		Assert.Contains(records, record => record is Record<MerchantShipPurchase>);
		Assert.True(MerchantCatalog.TryFind(EMerchantCatalog.ShipSupport, offering, before, out var offer));
		Assert.True(offer.Cost.TryGet(ResourceId.Credits, out var creditCost));
		Assert.True(offer.Cost.TryGet(ResourceId.ScrapAlloy, out var scrapCost));
		Assert.Equal(initialCredits - creditCost, engine.World.PlayerResources.GetBalance(ResourceId.Credits));
		Assert.Equal(initialScrap - scrapCost, engine.World.PlayerResources.GetBalance(ResourceId.ScrapAlloy));
	}

	[Fact]
	public void TryEnqueue_HullRepair_FailsWhenResourcesInsufficient()
	{
		var (engine, unitId, ship, _) = MerchantPurchaseTestHarness.CreateEngine(maps);
		ship.HullPoints = 1;
		var sim = engine.CreateSimulation();
		var before = ship.Clone();

		Assert.False(sim.TryEnqueue(CreateAction(
			unitId,
			engine.World,
			EMerchantCatalog.ShipSupport,
			MerchantPurchaseTestHarness.RepairHull,
			before)));
	}

	[Fact]
	public void CreateNewRun_AppliesRepairToRegistryWhenPurchaseCommits()
	{
		using var run = State.CreateNewRun(42);
		var shipId = run.PlayerParty.ShipIds[0];
		var ship = run.ShipRegistry.Get(shipId);
		ship.HullPoints = 1;
		run.ShipRegistry.Update(ship);
		SeedResources(run.StarSystem.Map, credits: 100, scrap: 100);
		var before = run.ShipRegistry.Get(shipId).Clone();
		var action = CreateAction(
			State.PlayerFleetUnitId,
			run.StarSystem.Map,
			EMerchantCatalog.ShipSupport,
			MerchantPurchaseTestHarness.RepairHull,
			before);
		var runtime = run.StarSystem.RuntimeFor(State.PlayerFleetUnitId);
		Assert.True(PurchaseActionDef.Instance.IsLegal(action, run.StarSystem.Map, runtime));

		run.StarSystem.CommitSetup(action);

		var updated = run.ShipRegistry.Get(shipId);
		Assert.Equal(before.Spec.MaxHullPoints, updated.HullPoints);
	}

	[Fact]
	public void TryEnqueue_ShieldRecharge_FailsWhenSupportMerchantOperatorForged()
	{
		var (engine, unitId, ship, _) = MerchantPurchaseTestHarness.CreateEngine(maps);
		ship.ShieldPoints.Fill(0);
		SeedResources(engine.World, credits: 500, scrap: 0);
		var sim = engine.CreateSimulation();
		var before = ship.Clone();

		Assert.False(sim.TryEnqueue(new PurchaseAction(
			unitId,
			SupplySystemPlan.Copper.TradeHubPoiId,
			DockyardFacilityId,
			MapFacilityOperators.ShopOperatorName(engine.World),
			EMerchantCatalog.ShipSupport,
			MerchantPurchaseTestHarness.RechargeAllShields,
			before)));
	}

	private static PurchaseAction CreateAction(
		string unitId,
		StarMap map,
		EMerchantCatalog catalog,
		MerchantCatalog.Offering offering,
		ShipInstance before) =>
		new(
			unitId,
			SupplySystemPlan.Copper.TradeHubPoiId,
			DockyardFacilityId,
			catalog == EMerchantCatalog.Weapons
				? MapFacilityOperators.ShopOperatorName(map)
				: MapFacilityOperators.ShieldOperatorName(map),
			catalog,
			offering,
			before);

	private static void SeedScrap(StarMap map, int amount)
	{
		var runtime = new ActorRuntime();
		new ChangeResourceEffect(TransactionSource.MerchantPurchase, ResourceBundle.Of(ResourceId.ScrapAlloy, amount))
			.Apply(map, runtime, "seed");
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

[StarSystemTestSuite]
public sealed class MerchantCommerceCharacterizationTests
{
	private const string DockyardFacilityId = "poi-trade-dockyard";

	[Fact]
	public void FighterDockyard_ListsExpectedUpgradeAndSupportQuotes()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);

		var weaponOffers = WeaponsCatalog.ListFor(ship);
		Assert.DoesNotContain(
			weaponOffers,
			o => o.Offering.Kind == MerchantCatalog.Kind.UpgradeMaxShields);
		Assert.Contains(
			weaponOffers,
			o => o.Offering == MerchantPurchaseTestHarness.FlakPortDamageUpgrade);

		var supportOffers = ShipSupportCatalog.ListFor(ship);
		var shieldOffer = supportOffers.Single(o => o.Offering.Kind == MerchantCatalog.Kind.UpgradeMaxShields);
		Assert.True(shieldOffer.Cost.TryGet(ResourceId.ScrapAlloy, out var shieldScrap));
		Assert.Equal(35, shieldScrap);

		var flakOffer = weaponOffers.Single(o => o.Offering == MerchantPurchaseTestHarness.FlakPortDamageUpgrade);
		Assert.True(flakOffer.Cost.TryGet(ResourceId.ScrapAlloy, out var flakScrap));
		Assert.Equal(40, flakScrap);

		ship.HullPoints = 1;
		var repairOffer = ShipSupportCatalog.ListFor(ship)
			.Single(o => o.Offering.Kind == MerchantCatalog.Kind.RepairHull);
		Assert.True(repairOffer.Cost.TryGet(ResourceId.Credits, out var hullCredits));
		Assert.True(repairOffer.Cost.TryGet(ResourceId.ScrapAlloy, out var hullScrap));
		Assert.Equal(ShipSupportCatalog.HullRepairCreditCost, hullCredits);
		Assert.Equal(ShipSupportCatalog.HullRepairScrapCost, hullScrap);

		ship = ShipInstance.FromCatalog("fighter-2", EType.Fighter);
		ship.ShieldPoints.Fill(0);
		var rechargeOffer = ShipSupportCatalog.ListFor(ship)
			.Single(o => o.Offering.Kind == MerchantCatalog.Kind.RechargeAllShields);
		Assert.True(rechargeOffer.Cost.TryGet(ResourceId.Credits, out var rechargeCredits));
		Assert.Equal(
			ship.TotalMaxShieldPoints * ShipSupportCatalog.ShieldRechargeCreditsPerPoint,
			rechargeCredits);
	}

	[Fact]
	public void CreateNewRun_CommittedDamageUpgradeMatchesRegistryAndBattleSpawn()
	{
		using var run = State.CreateNewRun(42);
		var shipId = run.PlayerParty.ShipIds[0];
		var ship = run.ShipRegistry.Get(shipId);
		var offering = MerchantPurchaseTestHarness.FlakPortDamageUpgrade;
		SeedScrap(run.StarSystem.Map, 500);
		var before = ship.Clone();
		var action = new PurchaseAction(
			State.PlayerFleetUnitId,
			SupplySystemPlan.Copper.TradeHubPoiId,
			DockyardFacilityId,
			MapFacilityOperators.ShopOperatorName(run.StarSystem.Map),
			EMerchantCatalog.Weapons,
			offering,
			before);

		run.StarSystem.CommitSetup(action);

		var registryShip = run.ShipRegistry.Get(shipId);
		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(State.PlayerFleetUnitId);
		var encounter = EngagementBattleFactory.Create(
			[playerFleet],
			run.ShipRegistry,
			seed: 9,
			id: "characterization-engagement");

		var spawn = encounter.Spawns.Single(s => s.Ship.Id == shipId);
		var portBefore = (FlakSpec)before.Spec.InstalledAbilities
			.First(a => a.MountedOn == ESpatialOrientation.Port).Spec;
		var portAfter = (FlakSpec)registryShip.Spec.InstalledAbilities
			.First(a => a.MountedOn == ESpatialOrientation.Port).Spec;
		Assert.Equal(portBefore.Damage + 1, portAfter.Damage);
		Assert.Equal(registryShip.Spec.InstalledAbilities, spawn.Ship.Spec.InstalledAbilities);
		Assert.Equal(registryShip.HullPoints, spawn.Ship.HullPoints);
		Assert.True(registryShip.ShieldPoints.Matches(spawn.Ship.ShieldPoints));
		Assert.True(MerchantCatalog.TryFind(EMerchantCatalog.Weapons, offering, registryShip, out var nextOffer));
		Assert.True(nextOffer.Cost.TryGet(ResourceId.ScrapAlloy, out var nextScrap));
		Assert.Equal(60, nextScrap);
	}

	private static void SeedScrap(StarMap map, int amount)
	{
		var runtime = new ActorRuntime();
		new ChangeResourceEffect(TransactionSource.MerchantPurchase, ResourceBundle.Of(ResourceId.ScrapAlloy, amount))
			.Apply(map, runtime, "seed");
	}
}
