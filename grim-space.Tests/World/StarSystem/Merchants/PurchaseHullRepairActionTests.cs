using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Poi.Concrete;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.Tests.World.StarSystem;
using GrimSpace.Tests.World.StarSystem.Poi;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

[StarSystemTestSuite]
public sealed class PurchaseHullRepairActionTests(StarMapFixture maps)
{
	private const string DockyardFacilityId = "poi-trade-dockyard";

	[Fact]
	public void Commit_DebitsResourcesAndEmitsPurchaseRecord()
	{
		var (engine, unitId, ship) = CreateEngine();
		ship.HullPoints = 1;
		SeedResources(engine.World, credits: 100, scrap: 100);
		var before = ship.Clone();
		var initialCredits = engine.World.PlayerResources.GetBalance(ResourceId.Credits);
		var initialScrap = engine.World.PlayerResources.GetBalance(ResourceId.ScrapAlloy);

		var records = engine.Commit(new PurchaseHullRepairAction(
			unitId,
			SupplySystemPlan.Copper.TradeHubPoiId,
			DockyardFacilityId,
			MapFacilityOperators.ShieldOperatorName(engine.World),
			before));

		Assert.Contains(records, record => record is Record<MerchantShipPurchase>);
		Assert.True(ShipSupportCatalog.TryQuoteHullRepair(before, out var cost));
		Assert.True(cost.TryGet(ResourceId.Credits, out var creditCost));
		Assert.True(cost.TryGet(ResourceId.ScrapAlloy, out var scrapCost));
		Assert.Equal(initialCredits - creditCost, engine.World.PlayerResources.GetBalance(ResourceId.Credits));
		Assert.Equal(initialScrap - scrapCost, engine.World.PlayerResources.GetBalance(ResourceId.ScrapAlloy));
	}

	[Fact]
	public void TryEnqueue_FailsWhenResourcesInsufficient()
	{
		var (engine, unitId, ship) = CreateEngine();
		ship.HullPoints = 1;
		var sim = engine.CreateSimulation();
		var before = ship.Clone();

		Assert.False(sim.TryEnqueue(new PurchaseHullRepairAction(
			unitId,
			SupplySystemPlan.Copper.TradeHubPoiId,
			DockyardFacilityId,
			MapFacilityOperators.ShieldOperatorName(engine.World),
			before)));
	}

	[Fact]
	public void TryEnqueue_FailsForStaleBeforeSnapshot()
	{
		var (engine, unitId, ship) = CreateEngine();
		ship.HullPoints = 1;
		SeedResources(engine.World, credits: 100, scrap: 100);
		var sim = engine.CreateSimulation();
		var stale = ship.Clone();
		stale.HullPoints = stale.Spec.MaxHullPoints;

		Assert.False(sim.TryEnqueue(new PurchaseHullRepairAction(
			unitId,
			SupplySystemPlan.Copper.TradeHubPoiId,
			DockyardFacilityId,
			MapFacilityOperators.ShieldOperatorName(engine.World),
			stale)));
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
		var action = new PurchaseHullRepairAction(
			State.PlayerFleetUnitId,
			SupplySystemPlan.Copper.TradeHubPoiId,
			DockyardFacilityId,
			MapFacilityOperators.ShieldOperatorName(run.StarSystem.Map),
			before);
		var runtime = run.StarSystem.RuntimeFor(State.PlayerFleetUnitId);
		Assert.True(PurchaseHullRepairDef.Instance.IsLegal(action, run.StarSystem.Map, runtime));

		MerchantShipPurchase? purchase = null;
		using var subscription = run.StarSystem.Subscribe<Record<MerchantShipPurchase>>(record =>
			purchase = record.Value);

		run.StarSystem.CommitSetup(action);
		Assert.NotNull(purchase);

		var updated = run.ShipRegistry.Get(shipId);
		Assert.Equal(before.Spec.MaxHullPoints, updated.HullPoints);
	}

	[Fact]
	public void TryEnqueue_FailsWhenShipSupportMerchantOperatorForged()
	{
		var (engine, unitId, ship) = CreateEngine();
		ship.HullPoints = 1;
		SeedResources(engine.World, credits: 100, scrap: 100);
		var sim = engine.CreateSimulation();
		var before = ship.Clone();

		Assert.False(sim.TryEnqueue(new PurchaseHullRepairAction(
			unitId,
			SupplySystemPlan.Copper.TradeHubPoiId,
			DockyardFacilityId,
			MapFacilityOperators.ShopOperatorName(engine.World),
			before)));
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

	private (Engine<StarMap, ActorRuntime> engine, string unitId, ShipInstance ship) CreateEngine(int seed = 42)
	{
		var map = maps.Fresh(seed);
		var unitId = map.FleetRegistry.All.First().State.Id;
		var ship = ShipInstance.FromCatalog("test-fighter", EType.Fighter);

		var runtimes = new ActorRuntimes<ActorRuntime>();
		runtimes.For(unitId);
		var engine = new Engine<StarMap, ActorRuntime>(map, runtimes);
		return (engine, unitId, ship);
	}
}
