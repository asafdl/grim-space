using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Dockyard;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Dockyard;

public sealed class PurchaseShieldRechargeActionTests(StarMapFixture maps)
{
	private const string DockyardFacilityId = "poi-trade-dockyard";

	[Fact]
	public void Commit_DebitsCreditsAndEmitsPurchaseRecord()
	{
		var (engine, unitId, ship) = CreateEngine();
		ship.ShieldPoints[ESpatialOrientation.Forward] = 0;
		SeedCredits(engine.World, 500);
		var before = ship.Clone();
		var initialCredits = engine.World.PlayerResources.GetBalance(ResourceId.Credits);

		var records = engine.Commit(new PurchaseShieldRechargeAction(
			unitId,
			SupplySystemPlan.Copper.TradeHubPoiId,
			DockyardFacilityId,
			before));

		Assert.Contains(records, record => record is Record<ShieldRechargePurchased>);
		Assert.True(DockyardShieldRecharge.TryQuote(before, out var cost));
		Assert.True(cost.TryGet(ResourceId.Credits, out var creditCost));
		Assert.Equal(initialCredits - creditCost, engine.World.PlayerResources.GetBalance(ResourceId.Credits));
	}

	[Fact]
	public void TryEnqueue_FailsWhenCreditsInsufficient()
	{
		var (engine, unitId, ship) = CreateEngine();
		ship.ShieldPoints[ESpatialOrientation.Forward] = 0;
		var sim = engine.CreateSimulation();
		var before = ship.Clone();

		Assert.False(sim.TryEnqueue(new PurchaseShieldRechargeAction(
			unitId,
			SupplySystemPlan.Copper.TradeHubPoiId,
			DockyardFacilityId,
			before)));
	}

	[Fact]
	public void Commit_RechargesSingleFaceOnly()
	{
		var (engine, unitId, ship) = CreateEngine();
		ship.ShieldPoints[ESpatialOrientation.Forward] = 0;
		ship.ShieldPoints[ESpatialOrientation.Port] = 0;
		SeedCredits(engine.World, 500);
		var before = ship.Clone();

		engine.Commit(new PurchaseShieldRechargeAction(
			unitId,
			SupplySystemPlan.Copper.TradeHubPoiId,
			DockyardFacilityId,
			before,
			ESpatialOrientation.Forward));

		Assert.True(DockyardShieldRecharge.TryQuoteFace(before, ESpatialOrientation.Forward, out var cost));
		Assert.True(cost.TryGet(ResourceId.Credits, out var creditCost));
		Assert.Equal(500 - creditCost, engine.World.PlayerResources.GetBalance(ResourceId.Credits));
	}

	[Fact]
	public void CreateNewRun_AppliesRechargeToRegistryWhenPurchaseCommits()
	{
		using var run = State.CreateNewRun(42);
		var shipId = run.PlayerParty.ShipIds[0];
		var ship = run.ShipRegistry.Get(shipId);
		ship.ShieldPoints[ESpatialOrientation.Forward] = 0;
		run.ShipRegistry.Update(ship);
		SeedCredits(run.StarSystem.Map, 500);
		var before = run.ShipRegistry.Get(shipId).Clone();
		var action = new PurchaseShieldRechargeAction(
			State.PlayerFleetUnitId,
			SupplySystemPlan.Copper.TradeHubPoiId,
			DockyardFacilityId,
			before);
		var runtime = run.StarSystem.RuntimeFor(State.PlayerFleetUnitId);
		Assert.True(PurchaseShieldRechargeDef.Instance.IsLegal(action, run.StarSystem.Map, runtime));

		ShieldRechargePurchased? purchase = null;
		using var subscription = run.StarSystem.Subscribe<Record<ShieldRechargePurchased>>(record =>
			purchase = record.Value);

		run.StarSystem.CommitSetup(action);
		Assert.NotNull(purchase);

		var updated = run.ShipRegistry.Get(shipId);
		Assert.Equal(before.Spec.MaxShieldPoints[ESpatialOrientation.Forward], updated.ShieldPoints[ESpatialOrientation.Forward]);
	}

	private static void SeedCredits(StarMap map, int amount)
	{
		var runtime = new ActorRuntime();
		new ChangeResourceEffect(TransactionSource.DockyardPurchase, ResourceBundle.Of(ResourceId.Credits, amount))
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
