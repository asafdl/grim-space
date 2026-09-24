using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
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
public sealed class PurchaseWeaponsUpgradeActionTests(StarMapFixture maps)
{
	private const string DockyardFacilityId = "poi-trade-dockyard";

	[Fact]
	public void Commit_DebitsScrapAndEmitsPurchaseRecord()
	{
		var (engine, unitId, ship, offerId) = CreateEngine();
		SeedScrap(engine.World, 100);
		var before = ship.Clone();
		var initialScrap = engine.World.PlayerResources.GetBalance(ResourceId.ScrapAlloy);

		var records = engine.Commit(new PurchaseWeaponsUpgradeAction(
			unitId,
			SupplySystemPlan.Copper.TradeHubPoiId,
			DockyardFacilityId,
			MapFacilityOperators.ShopOperatorName(engine.World),
			offerId,
			before));

		Assert.Contains(records, record => record is Record<MerchantShipPurchase>);
		Assert.True(WeaponsCatalog.TryQuote(offerId, ship, out var cost));
		Assert.True(cost.TryGet(ResourceId.ScrapAlloy, out var scrapCost));
		Assert.Equal(initialScrap - scrapCost, engine.World.PlayerResources.GetBalance(ResourceId.ScrapAlloy));
	}

	[Fact]
	public void TryEnqueue_FailsWhenScrapInsufficient()
	{
		var (engine, unitId, ship, offerId) = CreateEngine();
		var sim = engine.CreateSimulation();
		var before = ship.Clone();

		Assert.False(sim.TryEnqueue(new PurchaseWeaponsUpgradeAction(
			unitId,
			SupplySystemPlan.Copper.TradeHubPoiId,
			DockyardFacilityId,
			MapFacilityOperators.ShopOperatorName(engine.World),
			offerId,
			before)));
	}

	[Fact]
	public void TryEnqueue_FailsForStaleBeforeSnapshot()
	{
		var (engine, unitId, ship, offerId) = CreateEngine();
		SeedScrap(engine.World, 100);
		var sim = engine.CreateSimulation();
		var stale = ship.Clone();
		Assert.True(stale.TryWithUpgradedMaxShields(out var upgraded));
		stale.Spec = upgraded.Spec;
		stale.ShieldPoints = upgraded.ShieldPoints.Clone();

		Assert.False(sim.TryEnqueue(new PurchaseWeaponsUpgradeAction(
			unitId,
			SupplySystemPlan.Copper.TradeHubPoiId,
			DockyardFacilityId,
			MapFacilityOperators.ShopOperatorName(engine.World),
			offerId,
			stale)));
	}

	[Fact]
	public void CreateNewRun_AppliesUpgradeToRegistryWhenPurchaseCommits()
	{
		using var run = State.CreateNewRun(42);
		var shipId = run.PlayerParty.ShipIds[0];
		var ship = run.ShipRegistry.Get(shipId);
		var offerId = WeaponsCatalog.ListFor(ship).First().Id;
		SeedScrap(run.StarSystem.Map, 200);
		var before = ship.Clone();
		var action = new PurchaseWeaponsUpgradeAction(
			State.PlayerFleetUnitId,
			SupplySystemPlan.Copper.TradeHubPoiId,
			DockyardFacilityId,
			MapFacilityOperators.ShopOperatorName(run.StarSystem.Map),
			offerId,
			before);
		var runtime = run.StarSystem.RuntimeFor(State.PlayerFleetUnitId);
		Assert.True(PurchaseWeaponsUpgradeDef.Instance.IsLegal(action, run.StarSystem.Map, runtime));

		MerchantShipPurchase? purchase = null;
		using var subscription = run.StarSystem.Subscribe<Record<MerchantShipPurchase>>(record =>
			purchase = record.Value);

		run.StarSystem.CommitSetup(action);
		Assert.NotNull(purchase);

		var updated = run.ShipRegistry.Get(shipId);
		Assert.NotEqual(before.Spec, updated.Spec);
		Assert.DoesNotContain(offerId, WeaponsCatalog.ListFor(updated).Select(o => o.Id));
	}

	[Fact]
	public void TryEnqueue_FailsWhenWeaponsMerchantOperatorForged()
	{
		var (engine, unitId, ship, offerId) = CreateEngine();
		SeedScrap(engine.World, 100);
		var sim = engine.CreateSimulation();
		var before = ship.Clone();

		Assert.False(sim.TryEnqueue(new PurchaseWeaponsUpgradeAction(
			unitId,
			SupplySystemPlan.Copper.TradeHubPoiId,
			DockyardFacilityId,
			MapFacilityOperators.ShieldOperatorName(engine.World),
			offerId,
			before)));
	}

	private static void SeedScrap(StarMap map, int amount)
	{
		var runtime = new ActorRuntime();
		new ChangeResourceEffect(TransactionSource.MerchantPurchase, ResourceBundle.Of(ResourceId.ScrapAlloy, amount))
			.Apply(map, runtime, "seed");
	}

	private (Engine<StarMap, ActorRuntime> engine, string unitId, ShipInstance ship, string offerId) CreateEngine(
		int seed = 42)
	{
		var map = maps.Fresh(seed);
		var unitId = map.FleetRegistry.All.First().State.Id;
		var ship = ShipInstance.FromCatalog("test-fighter", EType.Fighter);
		var offerId = WeaponsCatalog.ListFor(ship).First().Id;

		var runtimes = new ActorRuntimes<ActorRuntime>();
		runtimes.For(unitId);
		var engine = new Engine<StarMap, ActorRuntime>(map, runtimes);
		return (engine, unitId, ship, offerId);
	}
}
